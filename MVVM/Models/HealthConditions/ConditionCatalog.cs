namespace ACS_View.MVVM.Models.HealthConditions
{
    internal static class ConditionCatalog
    {
        public static List<(string Category, string Condition)> All =>
        [
            // Cardíacas
            ("Cardíacas", "Hipertensão"),
            ("Cardíacas", "Insuficiência Cardíaca"),
            ("Cardíacas", "Arritmia"),

            // Endócrinas
            ("Endócrinas", "Diabetes Tipo 1"),
            ("Endócrinas", "Diabetes Tipo 2"),

            // Respiratórias
            ("Respiratórias", "Asma"),
            ("Respiratórias", "DPOC"),

            // Neurológicas
            ("Neurológicas", "Epilepsia"),
            ("Neurológicas", "AVC Hemorrágico"),
            ("Neurológicas", "AVC Isquêmico"),

            // Imunodeficientes
            ("Imunodeficientes", "HIV"),

            // Gestantes
            ("Outras", "Gestante"),
        ];
    }
}