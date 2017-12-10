using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ObjectPrinting
{
    public class PrintingConfig<TOwner>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Type[] FinalTypes =
        {
            typeof(int), typeof(double), typeof(float), typeof(string),
            typeof(DateTime), typeof(TimeSpan)
        };

        private readonly Dictionary<PropertyInfo, Func<object, string>> customPropertySerialization;

        private readonly Dictionary<Type, Func<object, string>> customTypeSerialization;

        private readonly List<PropertyInfo> excludingProprties;

        private readonly List<Type> excludingTypes;

        private readonly string newLine = Environment.NewLine;

        private readonly Dictionary<Type, CultureInfo> numericCulture;

        private readonly Dictionary<PropertyInfo, int> trimmingLength;

        public PrintingConfig()
        {
            excludingTypes = new List<Type>();
            excludingProprties = new List<PropertyInfo>();
            trimmingLength = new Dictionary<PropertyInfo, int>();
            customTypeSerialization = new Dictionary<Type, Func<object, string>>();
            numericCulture = new Dictionary<Type, CultureInfo>();
            customPropertySerialization = new Dictionary<PropertyInfo, Func<object, string>>();
        }


        public PrintingConfig<TOwner> Excluding<TPropType>()
        {
            excludingTypes.Add(typeof(TPropType));
            return this;
        }

        public PrintingConfig<TOwner> Excluding<TPropType>(Expression<Func<TOwner, TPropType>> memberSelector)
        {
            var propInfo = (PropertyInfo) ((MemberExpression) memberSelector.Body).Member;
            excludingProprties.Add(propInfo);
            return this;
        }

        public PropertyPrintingConfig<TOwner, TPropType> Printing<TPropType>() 
            => new PropertyPrintingConfig<TOwner, TPropType>(this, typeof(TPropType));

        public PropertyPrintingConfig<TOwner, TPropType> Printing<TPropType>
            (Expression<Func<TOwner, TPropType>> memberSelector)
        {
            var propInfo = (PropertyInfo) ((MemberExpression) memberSelector.Body).Member;
            return new PropertyPrintingConfig<TOwner, TPropType>(this, propInfo);
        }

        public string PrintToString(TOwner obj) => PrintToString(obj, 0);

        internal void AddCustomPropertySerialization(PropertyInfo property, Func<object, string> printRule) 
            => customPropertySerialization.Add(property, printRule);

        internal void AddCustomTypeSerialization(Type type, Func<object, string> printRule) 
            => customTypeSerialization.Add(type, printRule);

        internal void AddNumericCulture(Type type, CultureInfo culture) 
            => numericCulture.Add(type, culture);

        internal void AddTrimmingLength(PropertyInfo property, int maxLen) 
            => trimmingLength.Add(property, maxLen);

        private string PrintToString(object obj, int nestingLevel)
        {
            if (obj == null)
                return "null" + newLine;

            var type = obj.GetType();

            if (TryCovertNumericWithCulture(obj, type, out var result))
                return result;

            if (TryConvertTypeWithCustomParams(obj, type, out result))
                return result;

            if (TryConvertFinalType(obj, type, out result))
                return result;

            var identation = new string('\t', nestingLevel + 1);
            var sb = new StringBuilder();

            sb.AppendLine(type.Name);
            foreach (var propertyInfo in type.GetProperties())
            {
                if (IgnoreProperty(propertyInfo)) continue;
                if (!TryConvertPropertyWithCustomParams(obj, propertyInfo, out var propertyValue))
                    propertyValue = PrintToString(propertyInfo.GetValue(obj), nestingLevel + 1);
                propertyValue = TryTrimPropertyValue(propertyInfo, propertyValue);
                sb.Append(identation + propertyInfo.Name + " = " + propertyValue);
            }
            return sb.ToString();
        }

        private string TryTrimPropertyValue(PropertyInfo property, string propertyValue)
        {
            if (!trimmingLength.ContainsKey(property)) return propertyValue;
            return trimmingLength[property] < propertyValue.Length
                ? propertyValue.Substring(0, trimmingLength[property]) + newLine
                : propertyValue;
        }

        private bool TryConvertPropertyWithCustomParams(object obj, PropertyInfo property, out string result)
        {
            result = null;
            if (!customPropertySerialization.ContainsKey(property)) return false;
            result = customPropertySerialization[property](property.GetValue(obj)) + newLine;
            return true;
        }

        private bool TryConvertFinalType(object obj, Type type, out string result)
        {
            result = null;
            if (!FinalTypes.Contains(type)) return false;
            result = obj + newLine;
            return true;
        }

        private bool TryCovertNumericWithCulture(object obj, Type type, out string result)
        {
            result = null;
            if (!numericCulture.ContainsKey(type)) return false;
            result = ConverNumeric(obj, type) + newLine;
            return true;
        }

        private bool TryConvertTypeWithCustomParams(object obj, Type type, out string result)
        {
            result = null;
            if (!customTypeSerialization.ContainsKey(type)) return false;
            result = customTypeSerialization[type](obj) + newLine;
            return true;
        }

        private string ConverNumeric(object obj, Type type)
        {
            if (obj is IFormattable formattableObj)
                return formattableObj.ToString(null, numericCulture[type]);
            throw new InvalidCastException("Only IFormattable types supported");
        }

        private bool IgnoreProperty(PropertyInfo propertyInfo)
        {
            return excludingTypes.Contains(propertyInfo.PropertyType)
                   || excludingProprties.Contains(propertyInfo);
        }
    }
}