
using System;
using System.Linq;

// TODO: publish nuget package for MSBuildProjectBuilder to get types from it and so we can use the project builder for test project construction.

namespace CBT.Core.UnitTests
{
    public abstract class NameValuePair
    {
        public string Value { get; private set; }
        public string Name { get; private set; }
        public string Condition { get; private set; }
        public string Label { get; private set; }

        public NameValuePair(string name, string value, string condition, string label)
        {
            Value = value;
            Name = name;
            Condition = condition;
            Label = label;
        }
    }

    public class Item : NameValuePair
    {
        public ItemMetadata[] Metadata { get; private set; }
        public Item(string name, string value, string condition = null, string label = null, params ItemMetadata[] metadata) :
            base(name, value, condition ?? string.Empty, label ?? string.Empty)
        {
            Metadata = metadata;
        }

        public static implicit operator Item(string value)
        {
            string[] split = value.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

            return new Item(split.First(), split.Last());
        }
    }

    public class ItemMetadata : NameValuePair
    {
        public ItemMetadata(string name, string value = null, string condition = null, string label = null) :
            base(name, value ?? string.Empty, condition ?? string.Empty, label ?? string.Empty)
        { }

        public static implicit operator ItemMetadata(string value)
        {
            string[] split = value.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

            return new ItemMetadata(split.First(), split.Last());
        }
    }

    public class Property : NameValuePair
    {
        public Property(string name, string value = null, string condition = null, string label = null) :
            base(name, value ?? string.Empty, condition ?? string.Empty, label ?? string.Empty)
        { }

        public static implicit operator Property(string value)
        {
            string[] split = value.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

            return new Property(split.First(), split.Last());
        }
    }

    public class Import
    {
        public string Project { get; private set; }
        public string Condition { get; private set; }
        public string Label { get; private set; }

        public Import(string project, string condition = null, string label = null)
        {
            Project = project;
            Condition = condition ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public static implicit operator Import(string value)
        {
            return new Import(value);
        }
    }

}
