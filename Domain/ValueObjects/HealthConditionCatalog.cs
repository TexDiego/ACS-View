namespace ACS_View.Domain.ValueObjects
{
    public static class HealthConditionCatalog
    {
        public const string Gestante = "Gestante";
        public const string Diabetes = "Diabetes";
        public const string Hipertensao = "Hipertensão";
        public const string Tuberculose = "Tuberculose";
        public const string Hanseniase = "Hanseníase";
        public const string Acamado = "Acamado";
        public const string Domiciliado = "Domiciliado";
        public const string CondicaoMental = "Condição mental";
        public const string Fumante = "Fumante";
        public const string Alcoolatra = "Alcoólatra";
        public const string Deficiente = "Deficiente";
        public const string PortadorCancer = "Portador de câncer";
        public const string BolsaFamilia = "Beneficiário do Bolsa Família";
        public const string DependenteQuimico = "Dependente químico";
        public const string Imunodeficiente = "Imunodeficiente";

        public static readonly IReadOnlyList<string> Conditions =
        [
            Gestante,
            Diabetes,
            Hipertensao,
            Tuberculose,
            Hanseniase,
            Acamado,
            Domiciliado,
            CondicaoMental,
            Fumante,
            Alcoolatra,
            Deficiente,
            PortadorCancer,
            BolsaFamilia,
            DependenteQuimico,
            Imunodeficiente
        ];

        public static readonly IReadOnlyList<string> DiabetesTypes =
        [
            "Diabetes tipo 1",
            "Diabetes tipo 2",
            "Diabetes gestacional",
            "Diabetes sem tipo informado"
        ];

        public static string GetKey(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return string.Empty;
            }

            if (description.StartsWith(Diabetes, StringComparison.OrdinalIgnoreCase))
            {
                return Diabetes;
            }

            return Conditions.FirstOrDefault(condition =>
                string.Equals(condition, description, StringComparison.OrdinalIgnoreCase)) ?? description;
        }

        public static bool IsStandardCondition(string description)
        {
            return Conditions.Contains(GetKey(description), StringComparer.OrdinalIgnoreCase);
        }

        public static string GetDisplayName(string description)
        {
            return GetKey(description);
        }
    }
}
