using System;
using System.Reflection;

namespace ObjectPrinting
{
    public class PropertyPrintingConfig<TOwner, TPropType>
    {
        public PropertyPrintingConfig(PrintingConfig<TOwner> parentConfig, Type selectedType)
        {
            ParentConfig = parentConfig;
            SelectedType = selectedType;
        }

        public PropertyPrintingConfig(PrintingConfig<TOwner> parentConfig, PropertyInfo selectedProperty)
        {
            ParentConfig = parentConfig;
            SelectedProperty = selectedProperty;
        }

        internal PrintingConfig<TOwner> ParentConfig { get; }
        internal PropertyInfo SelectedProperty { get; }
        internal Type SelectedType { get; }

        public PrintingConfig<TOwner> Using(Func<TPropType, string> printRule)
        {
            if (SelectedType != null)
                ParentConfig.AddCustomTypeSerialization(typeof(TPropType), obj => printRule((TPropType) obj));
            else
                ParentConfig.AddCustomPropertySerialization(SelectedProperty, obj => printRule((TPropType) obj));
            return ParentConfig;
        }
    }
}