using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;


namespace Starward.Helpers.Enumeration;

/// <summary>
/// 用于 Enum 的单选绑定 ItemsSource
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class EnumItemsSource<T> : ObservableObject where T : struct, Enum
{


    public T DefaultValue { get; set; }


    public EnumItemsSource(T defaultValue = default)
    {
        DefaultValue = default;
        Items = Enum.GetValues<T>().Select(x => new EnumNameValue<T>(x)).ToList();
        Value = defaultValue;
    }



    [ObservableProperty]
    public partial List<EnumNameValue<T>> Items { get; set; }


    [ObservableProperty]
    public partial EnumNameValue<T>? SelectedItem { get; set; }
    partial void OnSelectedItemChanged(EnumNameValue<T>? value)
    {
        Value = value?.Value ?? DefaultValue;
    }


    [ObservableProperty]
    public partial T Value { get; set; }
    partial void OnValueChanged(T value)
    {
        SelectedItem = Items.FirstOrDefault(x => x.Value.Equals(value));
    }


    public class EnumNameValue<TEnum> where TEnum : struct, Enum
    {

        public string Name { get; set; }

        public TEnum Value { get; set; }


        public EnumNameValue(TEnum value)
        {
            Name = GetString(value);
            Value = value;
        }


        public override string ToString()
        {
            return Name;
        }


        private static string GetString(TEnum value)
        {
            string text = value.ToString();
            FieldInfo? field = typeof(TEnum).GetField(text);
            var locale = field?.GetCustomAttribute<LocalizationKeyAttribute>();
            if (locale is not null)
            {
                return Lang.ResourceManager.GetString(locale.Key)!;
            }
            var desc = typeof(TEnum).GetField(text)?.GetCustomAttribute<DescriptionAttribute>();
            if (desc is not null)
            {
                return desc.Description;
            }
            return text;
        }





    }


}
