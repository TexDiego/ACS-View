# Desempenho do cadastro de pacientes

Validação local em 10/10/2026. Alterações aplicadas ao checkout existente, preservando o trabalho local anterior.

## Fluxo e alterações

- `RegistersViewModel.AddPerson` navega para `addregister` sem parâmetros. `EditRecordAsync` passa somente `patientId`; a rota resolve uma página e um view model novos.
- Antes, `AddRegister.LoadPageDataAsync` carregava todas as subcategorias de CID antes de distinguir novo cadastro de edição. Agora, cadastro novo inicializa condições fixas e campos locais, com **zero chamadas aos serviços**.
- Edição carrega o paciente, SUS, telefones, benefício/responsável, insulinodependência, gestação, condições e vínculos de CID. Consulta somente as subcategorias vinculadas, por chave primária.
- `SQLitePatientRepository.GetByIdAsync` agora consulta os SUS somente do paciente e da conta atual, em vez de materializar todos os números da conta. O caminho de listagem existente foi preservado.
- `PregnancyService.GetDetailsByPatientIdAsync` conta filhos com `COUNT` filtrado por mãe e usuário, em vez de carregar todos os pacientes.
- O seletor de condições também deixava a primeira abertura dependente de materializar todo o catálogo. Agora usa a busca existente, limitada a cinco resultados, com debounce de 300 ms e descarte de buscas antigas. As seleções persistem entre buscas e são cópias independentes do formulário.
- Ao salvar, os CIDs selecionados são resolvidos pelo código antes de excluir os vínculos anteriores. Não há dependência de um catálogo previamente carregado.

## Medições reproduzíveis

```powershell
dotnet run --project Tests/ACSView.PatientLoadTests/ACSView.PatientLoadTests.csproj
```

O executável usa os view models e serviços SQLite de produção; substitui apenas a infraestrutura MAUI, diálogos, navegação, popups e atualização de notificações usada no salvamento. Cria um banco temporário com 10 mil pacientes da conta principal, dois pacientes de outra conta, 10 mil registros SUS e 15 mil CIDs. O paciente editado tem telefone, CID, diabetes, insulinodependência, benefício com responsável, gestação e dois filhos.

Cada medição contém uma execução inicial e 15 repetições. P95 é o percentil por posto mais próximo (máximo das 15 repetições). A execução abaixo ocorreu junto da compilação Android; há variação entre rodadas. Não são metas absolutas nem tempos de renderização em aparelho.

| Etapa | Inicial (ms) | Mediana (ms) | P95 (ms) |
| --- | ---: | ---: | ---: |
| Consulta anterior: catálogo inteiro de CID | 32,78 | 31,64 | 37,33 |
| Novo cadastro: inicialização local do view model | 1,83 | 0,02 | 0,55 |
| Paciente e SUS filtrados | 12,18 | 2,13 | 4,21 |
| Consulta anterior: todos os SUS da conta | 27,42 | 25,36 | 35,49 |
| Telefones | 5,78 | 0,47 | 1,06 |
| Bolsa Família e responsável | 4,27 | 2,66 | 4,18 |
| Insulinodependência | 3,78 | 0,63 | 1,07 |
| Detalhes gestacionais completos | 24,54 | 4,30 | 8,05 |
| Consulta anterior: todos os pacientes para contar filhos | 77,59 | 75,79 | 84,87 |
| Condições | 2,44 | 0,40 | 0,64 |
| Vínculos de CID | 2,70 | 0,49 | 0,75 |
| Uma subcategoria vinculada | 2,58 | 0,58 | 0,98 |
| Edição: carregamento completo do view model | 30,10 | 10,46 | 15,80 |
| Seletor: consulta limitada a cinco resultados | 9,99 | 6,66 | 9,33 |

As consultas anteriores são referências isoladas reproduzidas sobre a mesma base. Não foi medido o tempo completo de abertura da versão anterior. A consulta do seletor não inclui os 300 ms intencionais de debounce.

## Verificações

- **20 verificações de carregamento passaram**: novo formulário sem serviços, reconstrução dos dados de edição, isolamento entre contas, seleção/desseleção de CIDs entre buscas, cancelamento de buscas, criação, reabertura, edição e salvamento com CID, e integridade SQLite.
- **194 verificações de migração e persistência passaram** em `ACSView.MigrationTests`.
- **Compilação Android passou**, com zero erros e 49 avisos. O MSBuild exigiu execução fora do sandbox para localizar o SDK no registro; o teste de restauração também exigiu execução fora do sandbox para mover seu arquivo temporário.
- Dados reais e abertura visual/tátil em dispositivo físico não foram testados. As medições não incluem criação dos controles nativos, navegação Shell, layout ou renderização.

## Medição no aplicativo

Em execução Debug, filtrar a saída por `[Cadastro.Load]`. Há registros da construção do XAML/bindings, de cada serviço na edição, do total de edição e da preparação do cadastro novo. O cronômetro da construção começa dentro do construtor da página: não inclui resolução prévia da injeção de dependências nem criação posterior dos controles nativos.

Comparar no aparelho a primeira abertura e as seguintes, tanto em cadastro novo quanto em edição. Se o preparo dos dados estiver rápido e a abertura visual continuar lenta, os próximos pontos a medir são a navegação, criação de controles e layout do formulário.
