# Vacinação: atualização pelo manual de 2026

## Fonte e escopo

Manual do Profissional de Imunização, Kirky Dejane, edição 2026, 108 páginas,
anexo fornecido pelo usuário. A página 2 declara organização conforme a IN do
Calendário Nacional de Vacinação de 08/10/2026.
SHA-256: `BAEA8EAEA5E2E2910C78BCBE143818BB6EBEE3B99C4475A2BA7C18006519B930`.

Referência oficial consultada para conferência:
https://www.gov.br/saude/pt-br/vacinacao/publicacoes/instrucao-normativa-que-instrui-o-calendario-nacional-de-vacinacao-2026.pdf/view

O catálogo informa a fonte e as páginas das orientações. O sistema organiza o
cartão e documenta aplicações; as indicações marcadas para avaliação dependem
da conferência do histórico e da equipe de imunização. Não há decisão automática
de elegibilidade clínica, contraindicações individuais, campanhas locais ou CRIE.

## Correspondência com o documento

| Conteúdo | Páginas | Comportamento |
| --- | --- | --- |
| Transição VPC 20 / VPC 10 | 6, 33–38, 82 | Novas chaves para VPC 20 aos 2 meses e reforço/resgate; avaliar idade e histórico antes de indicar. Pneumo 10 anterior conserva seu produto. |
| Meningocócicas | 39–41, 52–54, 82, 94 | Men C aos 3 e 5 meses; reforço infantil com ACWY para avaliação; ACWY adolescente dos 11 aos 14 anos. |
| VIP | 27–29, 82, 85 | Segundo reforço aos 4 anos; seis meses mínimos antes de cada reforço; fora da rotina a partir de 5 anos. |
| Tríplice viral, tetraviral e varicela | 55–58, 61–63, 84 | Tríplice D2 e varicela D1 aos 15 meses; tetraviral como alternativa avaliada. Aplicação antiga de tetraviral cobre esses componentes sem inventar novas linhas. |
| Rotavírus e limites de idade | 30–32, 85 | D1 antes de 12 meses e D2 antes de 24 meses; doses fora do limite não entram em lote nem no indicador automático. |
| Covid-19 | 45–47, 82, 100, 103 | D3 infantil aos 9 meses; doses periódicas para avaliação, com orientação por grupo e sequência independente no idoso. |
| Influenza | 42–44, 82, 100, 103 | Registro por temporada; segunda aplicação infantil separada quando não há temporada anterior documentada. Conferir também registros externos. |
| HPV e dengue | 66–71, 94–95, 98 | HPV de rotina 9–14 anos; grupos especiais para avaliação. Dengue Takeda 10–14 anos, duas doses, intervalo de três meses e orientações de segurança. |
| dT, hepatite B e adultos | 21–23, 72–77, 95–101 | Conferir e completar esquemas, sem reiniciar; dT aos 14 anos na agenda oportuna e reforços conforme histórico. Indicações ocupacionais e especiais não viram pendência universal. |
| Gestação e VSR | 75–79, 103–104 | dTpa a partir de 20 semanas e VSR a partir de 28, por episódio de gestação; influenza sazonal e covid por gestação. DUM documentada informa a data da semana; sem DUM, solicitar avaliação sem inventar data. |

## Histórico e persistência

- Sem mudança de esquema SQLite e sem regravar aplicações existentes.
- Produtos substituídos usam novas chaves; chaves antigas e importadas desconhecidas
  continuam presentes no cartão quando há aplicação.
- Chaves sazonais: `@season-AAAA`; gestacionais: `@pregnancy-ID`;
  doses periódicas sucessivas: `@after-ID_DA_APLICAÇÃO_ANTERIOR`.
- Essas chaves são reconhecidas pelo catálogo somente para os tipos de ciclo suportados.
  Continuam armazenadas na tabela relacional de aplicações e isoladas por perfil.
- Datas desconhecidas permanecem desconhecidas. Uma gestação anterior não completa
  automaticamente a vacinação de uma nova gestação.
- Uma aplicação em indicação especial documenta aquela aplicação; a indicação e o
  restante de um esquema de múltiplas doses precisam de conferência profissional.

## Registro de novas aplicações periódicas

O cartão oferece **Registrar nova dose** para uma vacina periódica já aplicada,
separado de **Editar aplicação**. A nova aplicação começa com a data de hoje e
campos de lote/observação vazios, recebendo identidade própria `@application-GUID`.
Editar conserva a identidade da aplicação anterior.

No primeiro registro sazonal, a temporada segue o ano da data de aplicação:
uma influenza de julho de 2025 informada no cartão de 2026 permanece em 2025.
Registros da versão anterior cuja chave sazonal não coincide com a data continuam
intactos, mas deixam disponível uma aplicação para a temporada atual.
Covid periódica de adulto/adolescente/criança também permite novas aplicações;
o intervalo permanece sujeito à avaliação conforme o grupo.

## Alertas e lote

`RequiresAssessment` e `OutsideRoutine` não são contados como pendência/atraso
automático. O indicador infantil usa somente doses de rotina automática dentro
do limite de idade. Sua SQL considera a tetraviral anterior e a comprovação da
dose precedente quando necessária.

O lote revalida perfil, paciente, estado da aplicação, rotina, idade e datas dentro
da mesma transação. Doses posteriores exigem data anterior comprovada e intervalo
mínimo; penta D3 também exige quatro meses desde D1. A data padrão apresentada no
cartão considera a dose anterior conhecida. Exceções clínicas e registros históricos
continuam sendo conferidos individualmente, fora do lote automático.

A janela recomendada de uma dose não é apresentada como limite absoluto de
administração: o limite etário da rotina é uma informação separada.

## Validação

`VaccineManual2026Tests` usa somente pacientes e bancos sintéticos, cobrindo limites,
intervalos, ciclos, gestação, compatibilidade histórica, SQL do painel e isolamento
por perfil. Integra a suíte `ACSView.MigrationTests`. A suíte de domínio existente
continua verificando cálculo de status e construção de filtros.

Builds Windows/Android verificam compilação e XAML. A validação visual em execução,
a aplicação em dispositivo e a revisão clínica com cadernetas reais são verificações
distintas; não são comprovadas por esses testes.
