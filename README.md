# ACS View

**ACS View** é uma aplicação desenvolvida em .NET MAUI com o objetivo de auxiliar agentes comunitários de saúde no acompanhamento e organização de dados de pacientes. A ferramenta permite uma visualização prática e centralizada das informações, facilitando o trabalho diário desses profissionais.

---

## Funcionalidades Previstas

- Visualização rápida de pacientes cadastrados
- Acompanhamento de dados importantes como condições de saúde e informações demográficas
- Acompanhamento de situação vacinal
- Acompanhamento de pacientes cadastrados como beneficiários no Programa Bolsa Família
- Registro prático de anotações
- Criação de notificações personalizadas
- Consulta automática de endereço via CEP com integração à API pública [ViaCEP](https://viacep.com.br)
- Interface intuitiva e adaptada para uso em campo
- Armazenamento local de dados
- Registro de famílias visitadas com filtro de período
- Sugestões de visitas
- Consulta de CIDs com catálogo completo
- Criação de métricas personalizadas por unificação de condições (por exemplo: Hipertensos + Diabéticos)
- Filtrar registros de pacientes de forma personalizada
- Importação de dados
- Exclusão de dados em massa
- Persistência de dados por login

---

## Métricas Disponíveis

### Métricas Gerais

- Quantidade de pacientes
- Quantidade de residências
- Quantidade de famílias
- Idosos (60 anos ou mais)
- Crianças menores de 6 anos
- Mulheres de 25 a 64 anos
- Beneficiários do Bolsa Família
- Pacientes sem residência
- Residências vazias
- Pacientes inativos

### Métricas de Saúde

- Gestante
- Diabetes
- Hipertensão
- Dependentes de insulina
- Tuberculose
- Acamado
- Domiciliado
- Condição mental
- Fumante
- Usuário de álcool
- Portadores de deficiência
- Dependentes químicos
- CIDs específicos quando houver registro em algum paciente

> NOTA: é possível unir 2 condições para cruzar dados automaticamente. Está disponível para todas as condições de saúde e para algumas métricas gerais, evitando unificações que sempre resultam em 0 registros como idosos + crianças

---

## Tecnologias Utilizadas

- [.NET MAUI](https://learn.microsoft.com/pt-br/dotnet/maui/)
- C#
- SQLite (persistência local)
- Consumo de API REST com `HttpClient`
- Manipulação de JSON com `System.Text.Json`

---

## Planos Futuros

- Implementação de persistencia em nuvem. Banco de dados futuro ainda não definido





