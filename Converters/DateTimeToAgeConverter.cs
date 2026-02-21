using System.Globalization;

namespace ACS_View.Converters
{
    internal class DateTimeToAgeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime date)
                return CalcularIdade(date);

            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string CalcularIdade(DateTime dataNascimento, DateTime? dataReferencia = null)
        {
            var hoje = dataReferencia ?? DateTime.Today;

            if (dataNascimento > hoje)
                throw new ArgumentException("Data de nascimento não pode ser futura.");

            int anos = hoje.Year - dataNascimento.Year;
            int meses = hoje.Month - dataNascimento.Month;
            int dias = hoje.Day - dataNascimento.Day;

            if (dias < 0)
            {
                meses--;
                var ultimoMes = hoje.AddMonths(-1);
                dias += DateTime.DaysInMonth(ultimoMes.Year, ultimoMes.Month);
            }

            if (meses < 0)
            {
                anos--;
                meses += 12;
            }

            // Regra de exibição

            if (anos >= 1)
                return anos == 1 ? "1 ano" : $"{anos} anos";

            if (meses >= 1)
            {
                if (dias == 0)
                    return meses == 1 ? "1 mês" : $"{meses} meses";

                return $"{meses} {(meses == 1 ? "mês" : "meses")} e {dias} {(dias == 1 ? "dia" : "dias")}";
            }

            return dias == 1 ? "1 dia" : $"{dias} dias";
        }

    }
}