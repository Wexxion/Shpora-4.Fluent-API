using System.Globalization;

namespace ObjectPrinting
{
	public static class PropertyPrintingConfigExtensions
	{
		public static PrintingConfig<TOwner> TrimmedToLength<TOwner>
            (this PropertyPrintingConfig<TOwner, string> config, int maxLen)
        { 
            var parentConfig = config.ParentConfig;
		    parentConfig.TrimmingLength.Add(config.SelectedProperty, maxLen);
		    return parentConfig;
        }

	    private static PrintingConfig<TOwner> SetCultureForType<TOwner, TPropType>
	        (PropertyPrintingConfig<TOwner, TPropType> config, CultureInfo culture)
	    {
	        var parentConfig = config.ParentConfig;
            parentConfig.NumericCulture.Add(typeof(TPropType), culture);
            return parentConfig;
        }

        public static PrintingConfig<TOwner> Using<TOwner>
	        (this PropertyPrintingConfig<TOwner, int> config, CultureInfo culture) 
                => SetCultureForType(config, culture);

	    public static PrintingConfig<TOwner> Using<TOwner>
	        (this PropertyPrintingConfig<TOwner, long> config, CultureInfo culture) 
                => SetCultureForType(config, culture);

        public static PrintingConfig<TOwner> Using<TOwner>
	        (this PropertyPrintingConfig<TOwner, double> config, CultureInfo culture) 
                => SetCultureForType(config, culture);

	    public static PrintingConfig<TOwner> Using<TOwner>
	        (this PropertyPrintingConfig<TOwner, float> config, CultureInfo culture) 
                => SetCultureForType(config, culture);

    }
}