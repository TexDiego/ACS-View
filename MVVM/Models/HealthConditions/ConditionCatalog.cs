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
            ("Cardíacas", "Doença Arterial Coronária"),

            // Endócrinas
            ("Endócrinas", "Diabetes Tipo 1"),
            ("Endócrinas", "Diabetes Tipo 2"),
            ("Endócrinas", "Pré-Diabetes"),

            // Respiratórias
            ("Respiratórias", "Asma"),
            ("Respiratórias", "Bronquite Crônica"),
            ("Respiratórias", "Enfisema Pulmonar"),
            ("Respiratórias", "Tuberculose"),
            ("Respiratórias", "Pneumonia"),

            // Dermatológicas
            ("Dermatológicas", "Hanseníase"),
            ("Dermatológicas", "Rosácea"),
            ("Dermatológicas", "Vitiligo"),
            ("Dermatológicas", "Alopécia"),
            ("Dermatológicas", "Psoríase"),

            // Neurológicas
            ("Neurológicas", "Epilepsia"),
            ("Neurológicas", "AVC Hemorrágico"),
            ("Neurológicas", "AVC Isquêmico"),

            // Musculoesqueléticas
            ("Musculoesqueléticas", "Artrite"),
            ("Musculoesqueléticas", "Artrose"),
            ("Musculoesqueléticas", "Lombalgia"),
            ("Musculoesqueléticas", "Hérnia de Disco"),
            ("Musculoesqueléticas", "Tendinite"),
            ("Musculoesqueléticas", "Tendinite"),
            ("Musculoesqueléticas", "Bursite"),
            ("Musculoesqueléticas", "Fibromialgia"),
            ("Musculoesqueléticas", "Osteoporose"),

            // Oftalmológicas
            ("Oftalmológicas", "Miopia"),
            ("Oftalmológicas", "Hipermetropia"),
            ("Oftalmológicas", "Astigmatismo"),
            ("Oftalmológicas", "Glaucoma"),
            ("Oftalmológicas", "Presbiopia"),
            ("Oftalmológicas", "Catarata"),
            ("Oftalmológicas", "Ceratocone"),
            ("Oftalmológicas", "Retinopatia Diabética"),
            ("Oftalmológicas", "Retinopatia Hipertensiva"),

            // IST's
            ("IST's", "HIV"),
            ("IST's", "Sífilis"),
            ("IST's", "Gonorreia"),

            // Outras
            ("Outras", "Gestante"),
            ("Outras", "Acamado"),
            ("Outras", "Domiciliado"),

            // Vícios
            ("Vícios", "Fumante"),
            ("Vícios", "Dependente Químico"),
            ("Vícios", "Ludopata"),

            // Acessibilidade
            ("Deficiências", "Deficiênte Físico"),
            ("Deficiências", "Deficiênte Visual"),
            ("Deficiências", "Deficiênte Auditivo"),
            ("Deficiências", "Deficiênte Intelectual"),
            ("Deficiências", "Deficiênte "),
        ];
    }
}