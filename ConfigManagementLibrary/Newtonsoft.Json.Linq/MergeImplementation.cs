using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using Newtonsoft.Json.Linq;

namespace Daniels.Config
{
    public static class MergeImplementation
    {
        /// <summary>
        /// Merge the specified content into this <see cref="JToken"/>.
        /// </summary>
        /// <param name="content">The content to be merged.</param>
        public static void Merge(this JContainer target, object content)
        {
            if (content == null)
            {
                return;
            }

            target.ValidateContent(content);
            target.MergeItem(content, null);
        }

        /// <summary>
        /// Merge the specified content into this <see cref="JToken"/> using <see cref="JsonMergeSettings"/>.
        /// </summary>
        /// <param name="content">The content to be merged.</param>
        /// <param name="settings">The <see cref="JsonMergeSettings"/> used to merge the content.</param>
        public static void Merge(this JContainer target, object content, JsonMergeSettings settings)
        {
            if (content == null)
            {
                return;
            }

            target.ValidateContent(content);
            target.MergeItem(content, settings);
        }

        private static void ValidateContent(this JContainer target, object content)
        {
            if (content.GetType().IsSubclassOf(typeof(JToken)))
            {
                return;
            }
            if (target.IsMultiContent(content))
            {
                return;
            }

            throw new ArgumentException(String.Format("Could not determine JSON object type for type {0}.", "content"));
        }

        internal static bool IsMultiContent(this JContainer target, object content)
        {
            return (content is IEnumerable && !(content is string) && !(content is JToken) && !(content is byte[]));
        }

        internal static void MergeItem(this JContainer target, object content, JsonMergeSettings settings)
        {
            if (target is JArray)
                ((JArray)target).MergeItem(content, settings);
            if (target is JConstructor)
                ((JConstructor)target).MergeItem(content, settings);
            if (target is JObject)
                ((JObject)target).MergeItem(content, settings);
            if (target is JProperty)
                ((JProperty)target).MergeItem(content, settings);
        }

        internal static void MergeItem(this JArray target, object content, JsonMergeSettings settings)
        {
            IEnumerable a = (target.IsMultiContent(content) || content is JArray)
                ? (IEnumerable)content
                : null;
            if (a == null)
            {
                return;
            }

            target.MergeEnumerableContent(a, settings);
        }

        internal static void MergeItem(this JConstructor target, object content, JsonMergeSettings settings)
        {
            if (!(content is JConstructor))
            {
                return;
            }

            JConstructor c = content as JConstructor;
            if (c.Name != null)
            {
                target.Name = c.Name;
            }
            target.MergeEnumerableContent(c, settings);
        }

        internal static void MergeItem(this JObject target, object content, JsonMergeSettings settings)
        {
            if (!(content is JObject))
            {
                return;
            }

            JObject o = content as JObject;

            foreach (KeyValuePair<string, JToken> contentItem in o)
            {
                JProperty existingProperty = target.Property(contentItem.Key);

                if (existingProperty == null)
                {
                    target.Add(contentItem.Key, contentItem.Value);
                }
                else if (contentItem.Value != null)
                {
                    JContainer existingContainer = existingProperty.Value as JContainer;
                    if (!(existingContainer != null) || existingContainer.Type != contentItem.Value.Type)
                    {
                        if (!IsNull(contentItem.Value) || ((settings != null) ? (settings.MergeNullValueHandling == MergeNullValueHandling.Merge) : false))
                            existingProperty.Value = contentItem.Value;
                    }
                    else
                    {
                        existingContainer.Merge(contentItem.Value, settings);
                    }
                }
            }
        }

        internal static void MergeItem(this JProperty target, object content, JsonMergeSettings settings)
        {
            JToken value = null;
            JProperty c = content as JProperty;
            if (c != null)
                value = c.Value;

            if (value != null && value.Type != JTokenType.Null)
            {
                target.Value = value;
            }
        }

        private static bool IsNull(JToken token)
        {
            if (token.Type == JTokenType.Null)
            {
                return true;
            }

            JValue v = token as JValue;
            if (v != null && v.Value == null)
            {
                return true;
            }

            return false;
        }

        internal static void MergeEnumerableContent(this JContainer target, IEnumerable content, JsonMergeSettings settings)
        {
            switch ((settings != null) ? settings.MergeArrayHandling : MergeArrayHandling.Concat)
            {
                case MergeArrayHandling.Concat:
                    foreach (object item in content)
                    {
                        target.Add(CreateFromContent(item));
                    }
                    break;
                case MergeArrayHandling.Union:
#if HAVE_HASH_SET
                    HashSet<JToken> items = new HashSet<JToken>(target, EqualityComparer);

                    foreach (object item in content)
                    {
                        JToken contentItem = CreateFromContent(item);

                        if (items.Add(contentItem))
                        {
                            target.Add(contentItem);
                        }
                    }
#else
                    Dictionary<JToken, bool> items = new Dictionary<JToken, bool>(JToken.EqualityComparer);
                    foreach (JToken t in target)
                    {
                        items[t] = true;
                    }

                    foreach (object item in content)
                    {
                        JToken contentItem = CreateFromContent(item);

                        if (!items.ContainsKey(contentItem))
                        {
                            items[contentItem] = true;
                            target.Add(contentItem);
                        }
                    }
#endif
                    break;
                case MergeArrayHandling.Replace:
                    if (target == content)
                    {
                        break;
                    }
                    
                    target.RemoveAll();
                    foreach (object item in content)
                    {
                        target.Add(CreateFromContent(item));
                    }
                    break;
                case MergeArrayHandling.Merge:
                    int i = 0;
                    foreach (object targetItem in content)
                    {
                        if (i < target.Count)
                        {
                            JToken sourceItem = target[i];

                            if (sourceItem is JContainer)
                            {
                                JContainer existingContainer = sourceItem as JContainer;
                                existingContainer.Merge(targetItem, settings);
                            }
                            else
                            {
                                if (targetItem != null)
                                {
                                    JToken contentValue = CreateFromContent(targetItem);
                                    if (contentValue.Type != JTokenType.Null)
                                    {
                                        target[i] = contentValue;
                                    }
                                }
                            }
                        }
                        else
                        {
                            target.Add(CreateFromContent(targetItem));
                        }

                        i++;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("settings", "Unexpected merge array handling when merging JSON.");
            }
        }

        public static JToken CreateFromContent(object content)
        {
            if ((content != null) && content is JToken)
            {
                return (JToken)content;
            }

            return new JValue(content);
        }
    }
}