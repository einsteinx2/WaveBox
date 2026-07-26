using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;
using WaveBox.Core.ApiResponse.Subsonic;

namespace WaveBox.Subsonic {
    // Renders the Subsonic envelope as classic Subsonic XML using the mechanical XML<->JSON
    // mapping the protocol defines: scalar properties become attributes, nested objects and
    // lists become (repeated) child elements, and [SubsonicXmlText] properties become element
    // text.  Element/attribute names come from the same [JsonPropertyName] metadata the JSON
    // path uses, so the two wire formats cannot drift apart.
    //
    // Reflection over the DTOs is NativeAOT-safe because every DTO type is rooted in
    // SubsonicDtoRegistry (same pattern as the sqlite-net ORM and ModelTypeRegistry).
    public static class SubsonicXmlSerializer {
        public const string XmlNamespace = "http://subsonic.org/restapi";

        private class PropertyEntry {
            public string Name;
            public PropertyInfo Property;
        }

        private class TypeShape {
            public List<PropertyEntry> Attributes = new List<PropertyEntry>();
            public List<PropertyEntry> Elements = new List<PropertyEntry>();
            public PropertyInfo Text;
        }

        private static readonly ConcurrentDictionary<Type, TypeShape> shapes = new ConcurrentDictionary<Type, TypeShape>();

        public static string Serialize(SubsonicResponseBody body, bool indent) {
            StringBuilder sb = new StringBuilder();

            // Declared by hand: XmlWriter over a StringBuilder would declare utf-16, but the
            // response bytes are written as UTF-8 by IHttpProcessor.WriteText
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            if (indent) {
                sb.Append('\n');
            }

            XmlWriterSettings settings = new XmlWriterSettings {
                Indent = indent,
                OmitXmlDeclaration = true
            };

            using (XmlWriter writer = XmlWriter.Create(sb, settings)) {
                WriteObject(writer, "subsonic-response", body, XmlNamespace);
            }

            return sb.ToString();
        }

        private static void WriteObject(XmlWriter writer, string name, object obj, string ns = null) {
            if (ns != null) {
                writer.WriteStartElement(name, ns);
            } else {
                writer.WriteStartElement(name);
            }

            TypeShape shape = ShapeFor(obj.GetType());

            foreach (PropertyEntry attribute in shape.Attributes) {
                object value = attribute.Property.GetValue(obj);
                if (value != null) {
                    writer.WriteAttributeString(attribute.Name, FormatValue(value));
                }
            }

            if (shape.Text != null) {
                object text = shape.Text.GetValue(obj);
                if (text != null) {
                    writer.WriteString(FormatValue(text));
                }
            }

            foreach (PropertyEntry element in shape.Elements) {
                object value = element.Property.GetValue(obj);
                if (value == null) {
                    continue;
                }

                IEnumerable list = value as IEnumerable;
                if (list != null && !(value is string)) {
                    // Lists render as repeated elements; primitive items (extension versions,
                    // user folder ids) as text-content elements
                    foreach (object item in list) {
                        if (item == null) {
                            continue;
                        }
                        if (IsScalar(item.GetType())) {
                            writer.WriteElementString(element.Name, FormatValue(item));
                        } else {
                            WriteObject(writer, element.Name, item);
                        }
                    }
                } else {
                    WriteObject(writer, element.Name, value);
                }
            }

            writer.WriteEndElement();
        }

        // Reflection here is only reachable for DTO types rooted in SubsonicDtoRegistry,
        // which preserves all member metadata under trimming/NativeAOT
        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "All Subsonic DTO types are rooted in SubsonicDtoRegistry")]
        private static TypeShape BuildShape(Type type) {
            TypeShape shape = new TypeShape();

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (!property.CanRead || property.GetIndexParameters().Length != 0) {
                    continue;
                }

                JsonIgnoreAttribute ignore = property.GetCustomAttribute<JsonIgnoreAttribute>();
                if (ignore != null && ignore.Condition == JsonIgnoreCondition.Always) {
                    continue;
                }

                if (property.GetCustomAttribute<SubsonicXmlTextAttribute>() != null) {
                    shape.Text = property;
                    continue;
                }

                JsonPropertyNameAttribute jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>();
                string name = jsonName != null ? jsonName.Name : Char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);

                PropertyEntry entry = new PropertyEntry { Name = name, Property = property };
                Type valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (IsScalar(valueType)) {
                    shape.Attributes.Add(entry);
                } else {
                    shape.Elements.Add(entry);
                }
            }

            return shape;
        }

        private static TypeShape ShapeFor(Type type) {
            TypeShape shape;
            if (!shapes.TryGetValue(type, out shape)) {
                shape = BuildShape(type);
                shapes.TryAdd(type, shape);
            }
            return shape;
        }

        private static bool IsScalar(Type type) {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
        }

        private static string FormatValue(object value) {
            if (value is bool) {
                return (bool)value ? "true" : "false";
            }

            IFormattable formattable = value as IFormattable;
            if (formattable != null) {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }
    }
}
