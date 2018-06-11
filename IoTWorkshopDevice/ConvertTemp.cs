namespace IoTWorkshopDevice
{
    /// <summary>
    /// Temperature units conversion class
    /// </summary>
    public static class ConvertTemp
    {
        public static double ConvertCelsiusToFahrenheit( double c )
        {
            return ((9.0 / 5.0) * c) + 32;
        }

        public static double ConvertFahrenheitToCelsius( double f )
        {
            return (5.0 / 9.0) * (f - 32);
        }
    }
}
