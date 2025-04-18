﻿using SQLite;

namespace ACS_View.MVVM.Models
{
    public class HealthRecord
    {
        [PrimaryKey]
        public string SusNumber { get; set; }
        public int FamilyId { get; set; }
        public int HouseId { get; set; }
        public string Name { get; set; }
        public string? MotherName { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsPregnant { get; set; }
        public bool HasDiabetes { get; set; }
        public bool HasHypertension { get; set; }
        public bool HasTuberculosis { get; set; }
        public bool HasLeprosy { get; set; }
        public bool HasHIV { get; set; }
        public bool HasHeartDesease { get; set; }
        public bool HasKidneyDesease { get; set; }
        public bool HasLungsDesease { get; set; }
        public bool HasLiverDesease { get; set; }
        public bool IsBedridden { get; set; }
        public bool IsHomebound { get; set; }
        public bool HasMentalIllness { get; set; }
        public bool IsNeurodivergent { get; set; }
        public bool IsSmoker { get; set; }
        public bool IsAlcoholic { get; set; }
        public bool IsDrugAddicted { get; set; }
        public bool HasCancer { get; set; }
        public bool HasDisabilities { get; set; }
        public string? Observacao { get; set; }
        public bool BolsaFamilia { get; set; }

        // Vacinas

        public bool BCG { get; set; } = false;
        public bool HepatitisBAoNascer { get; set; } = false;
        public bool Penta1 { get; set; } = false;
        public bool VIP1 { get; set; } = false;
        public bool Pneumo10_1 { get; set; } = false;
        public bool VRH1 { get; set; } = false;
        public bool MeningoC1 { get; set; } = false;
        public bool Penta2 { get; set; } = false;
        public bool VIP2 { get; set; } = false;
        public bool Pneumo10_2 { get; set; } = false;
        public bool VRH2 { get; set; } = false;
        public bool MeningoC2 { get; set; } = false;
        public bool Penta3 { get; set; } = false;
        public bool VIP3 { get; set; } = false;
        public bool Covid1 { get; set; } = false;
        public bool Covid2 { get; set; } = false;
        public bool FebreAmarela1 { get; set; } = false;
        public bool Pneumo10_3 { get; set; } = false;
        public bool MeningoC3 { get; set; } = false;
        public bool TripliceViral { get; set; } = false;
        public bool DTP1 { get; set; } = false;
        public bool VIP4 { get; set; } = false;
        public bool HepatiteA { get; set; } = false;
        public bool TetraViral { get; set; } = false;
        public bool DTP2 { get; set; } = false;
        public bool FebreAmarela2 { get; set; } = false;
        public bool Varicela { get; set; } = false;
        public bool FebreAmarela3 { get; set; } = false;
        public bool Pneumo23 { get; set; } = false;
        public bool DT { get; set; } = false;
        public bool HPV { get; set; } = false;
        
        // Propriedades de exibição de vacinas
        public bool ShowRN => GetMonth(BirthDate) >= 0;
        public bool Show2Meses => GetMonth(BirthDate) >= 2;
        public bool Show3Meses => GetMonth(BirthDate) >= 3;
        public bool Show4Meses => GetMonth(BirthDate) >= 4;
        public bool Show5Meses => GetMonth(BirthDate) >= 5;
        public bool Show6Meses => GetMonth(BirthDate) >= 6;
        public bool Show7Meses => GetMonth(BirthDate) >= 7 ;
        public bool Show9Meses => GetMonth(BirthDate) >= 9;
        public bool Show1Ano => GetMonth(BirthDate) >= 12;
        public bool Show15Meses => GetMonth(BirthDate) >= 15;
        public bool Show4Anos => GetMonth(BirthDate) >= 48;
        public bool Show5Anos => GetMonth(BirthDate) >= 60;
        public bool Show7Anos => GetMonth(BirthDate) >= 72;
        public bool Show9Anos => GetMonth(BirthDate) >= 108;


        // Propriedades derivadas
        [Ignore]
        public bool HasObs => !string.IsNullOrEmpty(Observacao);

        [Ignore]
        public string Idade => CalcularIdadeCompleta(BirthDate);

        [Ignore]
        public bool IsOld => GetAge(BirthDate) >= 60;

        [Ignore]
        public bool IsBaby => GetAge(BirthDate) < 2;

        [Ignore]
        public bool IsChild => GetAge(BirthDate) <= 10;

        [Ignore]
        public bool IsDiabetesAndHypertension => HasDiabetes && HasHypertension;

        [Ignore]
        public string Endereco { get; set; } = "Sem endereço";

        [Ignore]
        public bool HasNothing =>
            IsPregnant && HasDiabetes && HasHypertension && HasTuberculosis && HasLeprosy &&
            HasHIV && HasHeartDesease && HasKidneyDesease && HasLungsDesease && HasLiverDesease &&
            IsBedridden && IsHomebound && HasMentalIllness && IsNeurodivergent &&
            IsSmoker && IsAlcoholic && IsDrugAddicted && HasCancer && HasDisabilities && BolsaFamilia;

        // Métodos para idade
        private static string CalcularIdadeCompleta(DateTime dataNascimento)
        {
            DateTime hoje = DateTime.Today;

            int anos = hoje.Year - dataNascimento.Year;
            int meses = hoje.Month - dataNascimento.Month;
            int dias = hoje.Day - dataNascimento.Day;

            if (dias < 0)
            {
                meses--;
                dias += DateTime.DaysInMonth(hoje.AddMonths(-1).Year, hoje.AddMonths(-1).Month);
            }

            if (meses < 0)
            {
                anos--;
                meses += 12;
            }

            if (anos == 0 && meses == 0 && dias == 0)
                return "Recém-nascido";

            if (anos == 0 && meses == 0)
                return dias == 1 ? "1 dia" : $"{dias} dias";

            if (anos == 0)
                return meses == 1 ? "1 mês" : $"{meses} meses";

            if (anos == 1)
                return meses == 0 ? "1 ano" : meses == 1 ? "1 ano e 1 mês" : $"1 ano e {meses} meses";

            return $"{anos} anos";
        }

        private static int GetAge(DateTime birthDate)
        {
            int age = DateTime.Today.Year - birthDate.Year;

            if (DateTime.Today.Month < birthDate.Month || (DateTime.Today.Month == birthDate.Month && DateTime.Today.Day < birthDate.Day))
            {
                age--;
            }

            return age;
        }

        private static int GetMonth(DateTime birthDate)
        {
            var today = DateTime.Today;

            int months = (today.Year - birthDate.Year) * 12 + today.Month - birthDate.Month;

            if (today.Day < birthDate.Day)
            {
                months--;
            }

            return Math.Max(months, 0); // Evita valor negativo
        }
    }
}
