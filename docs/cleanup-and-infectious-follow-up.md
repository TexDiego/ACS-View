# Limpeza e acompanhamentos de tuberculose / HIV

Foram removidos 11 arquivos sem referências ativas:

- Views/Desktop/DesktopPersonsInfo.xaml e DesktopPersonsInfo.xaml.cs: apresentação antiga substituída pelo popup compartilhado.
- Domain/Enums/IconPaths.cs: enum sem consumidores.
- Resources/Images/arrowleft.png, bin.png, loupe.png, pencil.png, plus.png, removeperson.png, slideralt.svg e syringe.png: sem referências textuais ou por nome-base nos fontes e configurações.

Também foram removidos os blocos de apresentação de gestação do popup antigo, a propriedade de abreviação de sexo sem consumidor no XAML atual, a opção CompactHeader sem usos e a propriedade HasMessage sem bindings. Os módulos de gestação ativos foram preservados.

Foram excluídos 117 logs antigos de compilação/teste na raiz. Os logs de validação desta alteração permanecem em tmp-infectious-*.log. Nenhum banco, backup, cadastro, migração histórica, captura de tela ou diretório de trabalho do usuário foi excluído.

## Arquitetura

A migração incremental 8 adiciona PatientInfectiousFollowUp e a definição explícita de HIV; mantém os IDs históricos das demais condições. Os acompanhamentos são opcionais, separados por paciente, perfil e condição, e estão incluídos em backup/restauração e limpeza de conta. Há validação de datas, situação e proprietário.

O cadastro agrupa Imunodeficiência / HIV na interface, mantendo duas marcações independentes. Cadastros antigos de Imunodeficiente não ganham HIV automaticamente; selecionar somente HIV não grava imunodeficiência. HIV não oferece tratamento concluído. Não são calculados diagnóstico, grau de imunossupressão ou condutas clínicas.

Tuberculose e HIV possuem ContentViews e ViewModels próprias, com editor compartilhado entre módulo e cadastro. O registro de módulos faz o pré-carregamento contextual; o cache e a invalidação usam as chaves individuais tuberculosis e hiv. Os painéis principal e adjacente ficam verticalmente centralizados, com alturas e rolagens independentes.

## Validação automatizada

- Windows e Android: compilação concluída sem erros.
- 374 verificações de migração e integração relacional.
- 106 verificações de backup/restauração.
- 34 verificações de cadastro, mais 14 dos módulos/editor.
- Os outros 11 projetos de teste disponíveis também foram executados com sucesso.
- git diff --check sem problemas nos arquivos alterados.

## Validação visual no Windows

Sessão autenticada, tema escuro: o popup principal manteve a mesma posição vertical antes e depois da abertura de Visitas. Foram inspecionados os formulários de tuberculose e HIV recolhidos e expandidos, incluindo HIV marcado sem imunodeficiência. Seis capturas foram salvas na pasta follow-up-checks das visualizações desta conversa. O formulário foi descartado sem salvar; os 625 cadastros existentes foram preservados.

A gravação e restauração dos novos acompanhamentos foram verificadas em testes automatizados. A inspeção visual dos módulos de tuberculose/HIV no popup com um cadastro persistido, aparelho Android, tema claro e diferentes tamanhos de janela permanece pendente.

