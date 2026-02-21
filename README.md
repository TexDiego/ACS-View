# ACS View

**ACS View** é uma aplicação desenvolvida em .NET MAUI com o objetivo de auxiliar agentes comunitários de saúde no acompanhamento e organização de dados de pacientes. A ferramenta permite uma visualização prática e centralizada das informações, facilitando o trabalho diário desses profissionais.

---

## Funcionalidades Previstas

- Visualização rápida de pacientes cadastrados
- Acompanhamento de dados importantes como condições de saúde e informações demográficas
- Acompanhamento de situação vacinal
- Registro prático de anotações
- Consulta automática de endereço via CEP com integração à API pública [ViaCEP](https://viacep.com.br)
- Interface intuitiva e adaptada para uso em campo
- Armazenamento local e online de dados
- Registro de famílias visitadas com filtro de período

---

## Tecnologias Utilizadas

- [.NET MAUI](https://learn.microsoft.com/pt-br/dotnet/maui/)
- C#
- SQLite (persistência local)
- Consumo de API REST com `HttpClient`
- Manipulação de JSON com `System.Text.Json`

---

## Arquiteturas e Estratégias

- SOLID
- MVVM (Model-View-ViewModel)

---

## Planos Futuros

- Refatoração completa do aplicativo, tornando mais robusto, escalável e melhor escrito para se adequar à Arquiteturas e Design Patterns
- Reformulação do modelo de dados de condições de saúde dos cadastros. Atualmente o modelo possui alguns itens criados por mim. Meu objetivo é migrar para o modelo fornecido pelo DataSus, que possui um catálogo completo de condições de saúde no formato CID-10. Veja mais sobre o modelo aqui: [DataSus](http://www2.datasus.gov.br/cid10/V2008/descrcsv.htm)
- Implementação de autenticação do usuário para login, inclusive com biometria
- Implementação de persistencia em nuvem. Banco de dados futuro ainda não definido





