namespace ACS_View.Views;

public partial class VaccineCalendarPage : ContentPage
{
    private static readonly GuideSection[] Sections =
    [
        new("Criança · do nascimento aos 9 anos", [
            new("Ao nascer", "BCG e hepatite B (dose neonatal)."),
            new("2 meses", "Penta D1, VIP D1, rotavírus D1 e pneumocócica conjugada D1."),
            new("3 meses", "Meningocócica C D1."),
            new("4 meses", "Penta D2, VIP D2, rotavírus D2 e pneumocócica conjugada D2."),
            new("5 meses", "Meningocócica C D2."),
            new("6 meses", "Penta D3, VIP D3, covid-19 D1 e influenza D1 na primeira vacinação."),
            new("7 meses", "Covid-19 D2 e influenza D2 na primeira vacinação, 30 dias após D1."),
            new("9 meses", "Covid-19 D3 no esquema Pfizer e febre amarela D1."),
            new("12 meses", "Tríplice viral D1, reforço pneumocócico e meningocócica ACWY."),
            new("15 meses", "DTP e VIP (primeiros reforços), tríplice viral D2, varicela D1 e hepatite A."),
            new("4 anos", "DTP e VIP (segundos reforços), febre amarela (reforço) e varicela D2."),
            new("9 anos", "HPV4 em dose única, com rotina até 14 anos."),
            new("Influenza anual", "Dos 6 meses até antes dos 6 anos. Após a primeira vacinação, uma dose por temporada."),
            new("Pneumocócica na transição", "VPC20 aos 2 meses, VPC10 aos 4 meses e VPC20 aos 12 meses. Na falta de VPC10, usar VPC20. Conferir histórico no resgate."),
            new("Limites de idade", "Rotavírus D1 antes dos 12 meses e D2 antes dos 24 meses. Men C antes dos 12 meses. BCG, VIP, hepatite A e covid-19 infantil de rotina antes dos 5 anos. Penta, DTP e varicela de rotina antes dos 7 anos. Veja exceções segundo o histórico.")
        ]),
        new("Adolescente · 10 a 19 anos", [
            new("10 a 14 anos", "Dengue (Takeda), duas doses separadas por 3 meses. HPV4 em dose única se ainda não registrada. ACWY dos 11 aos 14 anos, uma dose ou reforço."),
            new("14 anos", "Reforço de dT na agenda oportuna, conforme histórico."),
            new("15 a 19 anos", "Conferir resgate de HPV conforme a estratégia vigente no estado."),
            new("Em qualquer idade da faixa", "Completar hepatite B (três doses), dT (três doses com componente diftérico e tetânico), tríplice viral (duas doses) e febre amarela conforme histórico.")
        ]),
        new("Adulto · 20 a 59 anos", [
            new("Hepatite B e dT", "Completar hepatite B (0, 1 e 6 meses) e dT (três doses conforme histórico). Reforço de dT a cada 10 anos, com avaliação em ferimentos de risco."),
            new("Tríplice viral", "20 a 29 anos: duas doses. 30 a 59 anos: uma dose. Trabalhadores da saúde: duas doses conforme histórico."),
            new("Febre amarela", "Conferir histórico. Uma dose a partir dos 5 anos ou duas antes dessa idade geralmente completam o esquema. Uma dose antes dos 5 anos exige avaliação do reforço."),
            new("Influenza e covid-19", "Conferir as indicações para grupos prioritários e especiais.")
        ]),
        new("Idoso · 60 anos ou mais", [
            new("Rotina", "Influenza anual e covid-19 semestral. Conferir e completar hepatite B e dT, com reforço de dT conforme histórico."),
            new("Pneumo 20", "Avaliar idosos de 60 a 84 anos acamados ou institucionalizados e todos a partir de 85 anos, conforme histórico pneumocócico."),
            new("Avaliação individual", "Febre amarela e tríplice viral em situações específicas, conforme exposição e avaliação de risco.")
        ]),
        new("Gestação e puerpério", [
            new("Em cada gestação", "dTpa a partir da 20ª semana, VSR a partir da 28ª semana, influenza na temporada e covid-19 com intervalo mínimo de 6 meses da última dose."),
            new("Completar esquema", "Hepatite B e dT conforme histórico. A dTpa conta no esquema com componente tetânico."),
            new("Puerpério", "Conferir influenza até 45 dias após o parto se faltou na gestação, além das doses adiadas conforme avaliação da equipe."),
            new("Vacinas que exigem atenção", "HPV, dengue, tríplice viral e varicela ficam fora da rotina na gestação. Febre amarela exige avaliação em situações de alto risco.")
        ]),
        new("Grupos de risco e situações especiais", [
            new("Imunocomprometidos e comorbidades", "Covid-19 semestral para imunocomprometidos. Conferir estratégia para comorbidades e outros grupos especiais. Avaliar influenza anual e indicações do CRIE."),
            new("Povos indígenas", "Avaliar VPC20 a partir dos 5 anos sem comprovação de conjugada e varicela conforme histórico. Conferir influenza e covid-19 conforme estratégia."),
            new("Trabalhadores da saúde", "Conferir hepatite B, duas doses de tríplice viral, dTpa e reforços, varicela quando suscetível, influenza anual e covid-19 conforme estratégia."),
            new("HPV em grupos especiais", "Imunodeprimidos, usuários de PrEP nas faixas indicadas, vítimas de violência sexual e mulheres tratadas de NIC 2, NIC 3 ou AIS têm critérios próprios de idade e número de doses."),
            new("Caderneta atrasada", "Conferir doses documentadas, idade, limites e intervalos. Completar o esquema sem reiniciar. Tetraviral pode substituir tríplice viral com varicela em situações previstas no manual."),
            new("Exposição, viagens e surtos", "Conferir indicação de VIP para viagem internacional, febre amarela conforme destino, bloqueio de sarampo e reforço antitetânico em ferimentos.")
        ])
    ];

    public VaccineCalendarPage()
    {
        InitializeComponent();
        SectionPicker.ItemsSource = Sections.Select(section => section.Title).ToArray();
        SectionPicker.SelectedIndex = 0;
    }

    private async void OnSectionChanged(object sender, EventArgs e)
    {
        if (SectionPicker.SelectedIndex < 0) return;
        BindableLayout.SetItemsSource(GuideCards, Sections[SectionPicker.SelectedIndex].Entries);
        await GuideScroll.ScrollToAsync(0, 0, false);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SectionPicker.SelectedIndex = 0;
    }

    private sealed record GuideSection(string Title, VaccineCalendarEntry[] Entries);
}

public sealed record VaccineCalendarEntry(string Heading, string Details);
