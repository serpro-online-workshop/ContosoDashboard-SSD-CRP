---
lab:
    title: Exercício - Implementar um recurso de produto usando o GitHub Spec Kit
    description: Aprenda a adicionar novos recursos a aplicativos existentes usando os fluxos de trabalho do GitHub Spec Kit, do Visual Studio Code e do GitHub Copilot.
  duration: 75 minutes
  level: 400
  islab: true
  primarytopics:
    - GitHub
    - Visual Studio Code
---

# Implementar um recurso de produto usando o GitHub Spec Kit

O GitHub Spec Kit é um conjunto de ferramentas de código aberto que permite o desenvolvimento orientado por especificações (SSD), integrando especificações a assistentes de programação com IA, como o GitHub Copilot.

Neste exercício, você aprenderá a usar o GitHub Spec Kit para implementar um novo recurso em um aplicativo existente. Primeiro, inicializará o GitHub Spec Kit em um projeto .NET existente. Em seguida, usará os fluxos de trabalho do GitHub Spec Kit para criar os documentos de constituição, especificação, plano e tarefas de um novo recurso do aplicativo. Por fim, usará o fluxo de trabalho de implementação do GitHub Spec Kit para implementar uma versão inicial do MVP do aplicativo com o novo recurso.

Este exercício deve levar aproximadamente **75** minutos para ser concluído.

> **IMPORTANTE**: Para concluir este exercício, você precisa ter sua própria conta do GitHub e uma assinatura do GitHub Copilot. Se não tiver uma conta do GitHub, você poderá <a href="https://github.com/" target="_blank">criar</a> uma conta individual gratuita e usar o plano GitHub Copilot Free para concluir o exercício. Se o ambiente do laboratório der acesso a uma assinatura do GitHub Copilot Pro, GitHub Copilot Pro+, GitHub Copilot Business ou GitHub Copilot Enterprise, você poderá usar essa assinatura para concluir o exercício.

## Antes de começar

O ambiente do laboratório DEVE incluir os seguintes recursos:

- Git 2.48 ou posterior.
- SDK do .NET versão 9.0 ou posterior.
- Acesso a uma conta do GitHub com o GitHub Copilot habilitado.
- Visual Studio Code (versão 1.116 ou posterior) com a extensão C# Dev Kit.
- SQLite.
- Python 3.11 ou posterior.
- Gerenciador de pacotes uv.
- CLI Specify.

Para obter ajuda para configurar o ambiente do laboratório, abra o seguinte link em um navegador: <a href="https://go.microsoft.com/fwlink/?linkid=2345907" target="_blank">Configurar o ambiente de laboratório do GitHub Spec Kit</a>.

## Cenário do exercício

Você é desenvolvedor de software em uma empresa de consultoria. A empresa está adotando uma abordagem de desenvolvimento orientado por especificações (SDD) usando o GitHub Spec Kit e o GitHub Copilot no Visual Studio Code. Seu cliente, a Contoso Corporation, precisa que você adicione um novo recurso de "upload e gerenciamento de documentos" ao painel interno de funcionários (ContosoDashboard).

As partes interessadas de negócios da Contoso documentaram os requisitos do novo recurso. Em essência, os funcionários precisam poder carregar documentos relacionados ao trabalho, organizá-los por categoria e projeto e compartilhá-los com os membros da equipe. O novo recurso deve se integrar perfeitamente à interface existente do painel, mantendo os padrões de segurança e conformidade.

Você precisa seguir uma metodologia de desenvolvimento orientado por especificações com o GitHub Spec Kit para implementar o novo recurso. O GitHub Spec Kit permite criar os arquivos constitution.md, spec.md, plan.md e tasks.md que orientam o processo de desenvolvimento. A abordagem SDD com o GitHub Spec Kit garante que a implementação esteja alinhada aos requisitos de negócios e às restrições organizacionais.

Este exercício inclui as seguintes tarefas:

1. Importar o repositório ContosoDashboard e inicializar o GitHub Spec Kit.
1. Revisar o projeto ContosoDashboard e os arquivos do GitHub Spec Kit.
1. Gerar uma constituição com base nos arquivos do repositório.
1. Criar a especificação do recurso usando os requisitos das partes interessadas e a constituição.
1. Atualizar a especificação com os requisitos esclarecidos.
1. Gerar o plano técnico usando a especificação e a constituição.
1. Gerar o arquivo de tarefas usando a especificação, o plano e a constituição.
1. Implementar as tarefas necessárias para um aplicativo MVP.

## Importar o repositório ContosoDashboard e inicializar o GitHub Spec Kit

O GitHub Importer pode ser usado para criar uma cópia de um repositório existente em sua própria conta do GitHub, dando a você controle total sobre a cópia importada.

Nesta tarefa, você importará o repositório do aplicativo ContosoDashboard para sua conta do GitHub e inicializará o GitHub Spec Kit no diretório do projeto.

Siga estas etapas para concluir a tarefa:

1. Abra uma janela do navegador e acesse GitHub.com.

    Você pode entrar na sua conta do GitHub usando esta URL: <a href="https://github.com/login" target="_blank">Entrar no GitHub</a>.

1. Entre na sua conta do GitHub e abra a guia dos seus repositórios.

    Para abrir a guia dos seus repositórios, clique no ícone do perfil no canto superior direito e selecione **Repositories**.

1. Na guia Repositories, selecione o botão **New**.

1. Na seção **Create a new repository**, selecione **Import a repository**.

1. Na página **Import your project to GitHub**, em **Your source repository details**, insira a URL a seguir para o repositório de origem:

    ```plaintext
    https://github.com/MicrosoftLearning/ContosoDashboard-SSD.git
    ```

1. Na seção **Your new repository details**, selecione seu nome de usuário do GitHub na lista suspensa **Owner**.

1. Insira **ContosoDashboard** no campo **Repository name**.

    O GitHub verifica automaticamente se o nome do repositório está disponível. Se o nome já estiver em uso, acrescente um sufixo exclusivo (por exemplo, suas iniciais ou um número aleatório) para torná-lo único.

1. Para criar um repositório privado, selecione **Private** e, em seguida, **Begin import**.

    O GitHub usa o processo de importação para criar o novo repositório na sua conta. A importação pode levar um ou dois minutos. Aguarde até que ela seja concluída.

    > **IMPORTANTE**: Se estiver usando o plano GitHub Copilot Free, crie o repositório como **Public** para garantir acesso aos recursos do GitHub Copilot. Se você tiver uma assinatura Pro, Pro+, Business ou Enterprise, poderá criar o repositório como **Private**.

    O GitHub exibe um indicador de progresso e avisa quando a importação é concluída.

1. Quando a importação terminar, abra o novo repositório.

    Um link para o repositório deverá ser exibido. O repositório deverá estar em: `https://github.com/YOUR-USERNAME/ContosoDashboard`.

    Agora você pode clonar localmente o repositório ContosoDashboard e inicializar o GitHub Spec Kit no diretório do projeto.

1. Na página do repositório ContosoDashboard, selecione o botão **Code** e copie a URL HTTPS.

    A URL deverá ser semelhante a: `https://github.com/YOUR-USERNAME/ContosoDashboard.git`.

1. Abra uma janela do terminal no ambiente de desenvolvimento e acesse o local onde deseja criar o clone local do repositório.

    Por exemplo:

    Abra uma janela do terminal (Prompt de Comando, PowerShell ou Terminal) e execute:

    ```powershell
    cd C:\TrainingProjects
    ```

    Substitua `C:\TrainingProjects` pelo local de sua preferência. Você pode usar qualquer diretório em que tenha permissão de gravação e criar uma nova pasta, se necessário.

1. Para clonar o repositório ContosoDashboard, insira o seguinte comando:

    Antes de executar o comando, substitua `YOUR-USERNAME` pelo seu nome de usuário real do GitHub.

    ```powershell
    git clone https://github.com/YOUR-USERNAME/ContosoDashboard.git
    ```

    Durante a clonagem, talvez seja solicitado que você se autentique com suas credenciais do GitHub. Você pode fazer isso pelo navegador.

1. Para acessar o diretório ContosoDashboard, insira o seguinte comando:

    ```powershell
    cd ContosoDashboard
    ```

    > **IMPORTANTE**: O GitHub Spec Kit deve ser inicializado no diretório raiz do repositório clonado. Por exemplo, se você clonou o repositório em `C:\TrainingProjects\ContosoDashboard`, execute o comando `specify init` a partir desse diretório.

1. Para inicializar o GitHub Spec Kit no projeto existente, insira o seguinte comando:

    ```powershell
    specify init --here --integration copilot --script ps
    ```

    O comando usa os seguintes componentes:

    - `--here` - Inicializa o GitHub Spec Kit no diretório atual (o projeto ContosoDashboard existente).
    - `--integration copilot` - Configura o projeto para o GitHub Copilot.
    - `--script ps` - Usa scripts do PowerShell (use `--script sh` para bash/zsh no macOS/Linux).

    Se estiver usando macOS ou Linux, substitua `--script ps` por `--script sh`.

    A CLI detecta um repositório Git existente ("Current directory isn't empty") e solicita confirmação para continuar.

1. Insira **y** para continuar a inicialização.

    A CLI irá:

    - Criar arquivos de prompt de agente nos diretórios `.github/agents/` e `.github/prompts/`.
    - Criar arquivos de modelo nos diretórios `.specify/memory/` e `.specify/templates/`.
    - Criar arquivos de script no diretório `.specify/scripts/powershell/`.
    - Atualizar ou criar um arquivo settings.json no diretório `.vscode/`.
    - Preservar todos os arquivos existentes do aplicativo.
    - Exibir uma mensagem de sucesso ("Project ready").
    - Sugerir algumas próximas etapas opcionais.

## Revisar o projeto ContosoDashboard e os arquivos do GitHub Spec Kit

O GitHub Spec Kit funciona com o GitHub Copilot por meio da interface de chat do Visual Studio Code. Quando você executa "specify init --integration copilot" no diretório do projeto, o conjunto de ferramentas configura o espaço de trabalho para reconhecer os comandos "/speckit.*".

Nesta tarefa, você explorará os arquivos do projeto ContosoDashboard no Visual Studio Code, verificará se o GitHub Spec Kit foi inicializado corretamente e, em seguida, fará *push* dos arquivos atualizados para o repositório do GitHub.

Siga estas etapas para concluir a tarefa:

1. Abra o projeto ContosoDashboard no Visual Studio Code.

    Por exemplo, se a janela do terminal ainda estiver aberta, você poderá usar este comando para abrir o projeto:

    ```powershell
    code .
    ```

    O comando `code .` abre o diretório atual (ContosoDashboard) no Visual Studio Code.

    Aguarde o Visual Studio Code carregar o projeto por completo.

1. Reserve um minuto para se familiarizar com a estrutura do projeto.

    Use o modo de exibição Explorer do Visual Studio Code para expandir as pastas do aplicativo. A estrutura deverá ser semelhante ao exemplo a seguir:

    ```plaintext
    CONTOSODASHBOARD (root)
    ├── .github/
    │   ├── agents/                 (fluxos executáveis do GitHub Spec Kit que podem ser iniciados por comandos)
    │   └── prompts/                (arquivos de prompt do GitHub Spec Kit com instruções detalhadas para cada fluxo de agente)
    ├── .specify/                   (GitHub Spec Kit configuration)
    │   ├── extensions/             (o GitHub Spec Kit armazena pacotes de extensão instalados e seus recursos - comandos, modelos, hooks e configurações - que adicionam recursos opcionais ao fluxo principal do Specify.)
    │   ├── integrations/           (o GitHub Spec Kit armazena o estado de integração ativo do projeto com agentes de IA e seus manifestos para que o Specify possa instalar, alternar, atualizar ou desinstalar com segurança a configuração de comandos específicos de agentes.)
    │   ├── memory/                 (o GitHub Spec Kit armazena a constituição do projeto, que define os princípios fundamentais e as regras de governança que todos os recursos devem seguir)
    │   ├── scripts/powershell/     (o GitHub Spec Kit usa utilitários de automação (scripts) para criar recursos, configurar planos e gerenciar o fluxo de trabalho da especificação)
    │   └── templates/              (o GitHub Spec Kit fornece formatos Markdown padronizados para especificações, planos, tarefas e listas de verificação, garantindo documentação consistente entre os recursos)
    ├── ContosoDashboard/           (Main application folder)
    │   ├── Data/                   (ApplicationDbContext.cs)
    │   ├── Models/                 (Announcement, Notification, Project, ProjectMember, TaskComment, TaskItem, User)
    │   ├── Pages/                  (_Host, _Imports, Index, Login, Logout, Notifications, Profile, ProjectDetails, Projects, Tasks, Team)
    │   ├── Properties/             (launchSettings.json)
    │   ├── Services/               (CustomAuthenticationStateProvider, DashboardService, NotificationService, ProjectService, TaskService, UserService)
    │   ├── Shared/                 (_Imports, MainLayout, NavMenu, RedirectToLogin)
    │   ├── wwwroot/                (Static files, CSS)
    │   └── Program.cs              (App configuration)
    ├── StakeholderDocs/            (Business requirements)
    └── README.md                   (Application documentation)
    ```

1. Verifique se o modo de exibição Chat do GitHub Copilot está aberto.

    O uso de um dos modelos de linguagem mais recentes pode melhorar a qualidade das respostas. Este exercício de laboratório foi testado originalmente com os modelos GPT-5 e Claude Sonnet 4.5. Os resultados foram comparáveis entre os dois modelos.

1. No modo Chat, digite **/speckit** para verificar se os comandos do GitHub Spec Kit estão disponíveis.

    Você deverá ver sugestões de preenchimento automático com os comandos disponíveis. Por exemplo:

    - `/speckit.analyze` - Auditar planos de implementação.
    - `/speckit.checklist` - Validar a completude da especificação.
    - `/speckit.clarify` - Refinar especificações por meio de perguntas e respostas.
    - `/speckit.constitution` - Definir os princípios que regem o projeto.
    - `/speckit.implement` - Executar a implementação.
    - `/speckit.plan` - Gerar planos técnicos de implementação.
    - `/speckit.specify` - Criar especificações de recursos.
    - `/speckit.tasks` - Dividir o trabalho em tarefas aplicáveis.
    - `/speckit.taskstoissues` - Converter as tarefas de tasks.md em issues do GitHub.

    > **Observação**: Se os comandos '/speckit.' não aparecerem, tente fechar e reabrir o projeto no Visual Studio Code.

    **Solução de problemas**: se encontrar algum problema:

    - **"specify command not found"**: Verifique se concluiu a Tarefa 1 e instalou a CLI Specify. Execute `specify version` para confirmar a instalação.
    - **Erros de permissão negada**: No Windows, verifique se está executando o PowerShell com as permissões adequadas. No macOS/Linux, confira as permissões dos arquivos.
    - **Erros ao clonar com Git**: Verifique se entrou no GitHub e se tem acesso ao repositório importado.
    - **Comandos do GitHub Spec Kit não aparecem**: Verifique se `.github/prompts/` existe na raiz do espaço de trabalho. Tente recarregar o Visual Studio Code.

1. Peça ao GitHub Copilot que explique o projeto atual e os arquivos do GitHub Spec Kit.

    Por exemplo, insira o seguinte prompt no modo Chat:

    ```plaintext
    Revise a base de código atual. Explique os recursos do aplicativo ContosoDashboard e a finalidade dos arquivos do GitHub Spec Kit localizados nos diretórios .github\ e .specify\.
    ```

1. Reserve alguns minutos para revisar a resposta do GitHub Copilot.

    A resposta do GitHub Copilot deverá resumir os recursos do aplicativo e explicar a finalidade dos arquivos do GitHub Spec Kit.

    Você também pode consultar o arquivo README.md do projeto para ver uma descrição dos recursos atuais do aplicativo, do sistema de autenticação simulado e da implementação de segurança.

1. Abra o arquivo **ContosoDashboard/ContosoDashboard.csproj** no editor.

    Observe o seguinte:

    - O arquivo do projeto especifica o .NET 8 como estrutura de destino. Se o ambiente de desenvolvimento tiver outra versão do SDK do .NET instalada (.NET 9 ou .NET 10), será necessário atualizar o arquivo do projeto para usar essa versão.
    - O arquivo do projeto deve usar SQLite como provedor de banco de dados para desenvolvimento local, independentemente da arquitetura do computador.

1. Verifique se o arquivo ContosoDashboard.csproj especifica a versão do .NET instalada no ambiente de desenvolvimento.

    Você pode usar o GitHub Copilot para atualizar o arquivo do projeto. Por exemplo, para atualizar o arquivo ContosoDashboard.csproj para o .NET 10, insira o seguinte prompt no modo Chat:

    ```plaintext
    Tenho o SDK do .NET 10 instalado. Meu projeto foi criado com o .NET 8. Atualize o arquivo .csproj para o .NET 10 e confirme que o projeto compila corretamente.
    ```

    > **OBSERVAÇÃO**: O aplicativo ContosoDashboard foi desenvolvido com o .NET 8. Se o SDK do .NET 8 não estiver instalado no ambiente de desenvolvimento, mas você tiver o SDK do .NET 9 ou .NET 10, o arquivo ContosoDashboard.csproj deverá ser atualizado para usar a versão instalada do .NET antes de compilar e executar o aplicativo.

1. Verifique se o projeto está configurado para usar SQLite como provedor de banco de dados.

    O SQLite deve ser usado para o desenvolvimento local em todas as plataformas. Confira se os seguintes arquivos estão configurados para esse provedor:

    - ContosoDashboard.csproj: atualize a referência ao pacote do provedor de banco de dados.
    - Program.cs: atualize a configuração do contexto do banco de dados.
    - appsettings.json: atualize a cadeia de conexão.

    Você pode usar o GitHub Copilot para configurar os arquivos do projeto (ContosoDashboard.csproj, Program.cs e appsettings.json) para SQLite. Por exemplo, insira o seguinte prompt no modo Chat:

    ```plaintext
    Configure a base de código para usar SQLite como provedor de banco de dados em todos os ambientes de desenvolvimento.
    ```

    > **OBSERVAÇÃO**: Esta configuração se aplica a computadores com processadores ARM e x64 e a Macs.

1. No modo Explorer, clique com o botão direito do mouse em **ContosoDashboard** e selecione **Open in Integrated Terminal**.

    O prompt do terminal deverá abrir no diretório do projeto ContosoDashboard. Por exemplo:

    ```plaintext
    PS C:\TrainingProjects\ContosoDashboard\ContosoDashboard>
    ```

1. Para compilar e executar o aplicativo, insira os seguintes comandos:

    ```dotnetcli
    dotnet restore
    dotnet build
    dotnet run
    ```

    Algumas mensagens de **aviso** serão exibidas durante a compilação e a execução do aplicativo, mas não deverá haver erros.

1. Aguarde o início do aplicativo, abra uma janela do navegador e acesse a URL localhost exibida no terminal.

    Você deverá ver uma URL semelhante a `https://localhost:5000`.

    Ao abrir o aplicativo ContosoDashboard no navegador, você deverá ver a página de entrada.

1. Na página de entrada do ContosoDashboard, selecione **Ni Kang (Employee)** na lista suspensa e, em seguida, **Login**.

1. Reserve um minuto para explorar o aplicativo ContosoDashboard.

    O aplicativo inclui recursos básicos de painel, como acompanhamento de projetos, gerenciamento de tarefas, notificações e gerenciamento do perfil do usuário.

    É importante verificar se o aplicativo está funcionando e explorar os recursos e comportamentos existentes antes de projetar e desenvolver um novo recurso. No entanto, não é necessário gastar muito tempo explorando a interface; basta observar as funcionalidades básicas por um minuto.

1. Saia do aplicativo e feche a guia do navegador.

    Você pode minimizar a janela do navegador, mas mantenha-a aberta por enquanto.

1. No painel Terminal do Visual Studio Code, pressione **Ctrl+C** para interromper o aplicativo em execução e feche o terminal.

1. Use o modo de exibição Source Control do Visual Studio Code para confirmar e, em seguida, enviar/sincronizar os arquivos atualizados do projeto.

    Por exemplo:

    - Selecione o ícone Source Control na barra de atividades à esquerda.
    - Insira uma mensagem de commit, como: "Adicionar arquivos do GitHub Spec Kit ao projeto ContosoDashboard".
    - Selecione o ícone de marca de seleção para confirmar as alterações (e selecione Yes para prepará-las, se solicitado).
    - Selecione **Sync Changes** para enviar o commit ao GitHub (e selecione OK, se solicitado).

    Ao enviar os arquivos do GitHub Spec Kit para o repositório, você poderá acompanhar o processo de desenvolvimento orientado por especificações.

1. Abra o repositório do GitHub em uma janela do navegador e verifique se o envio foi concluído com sucesso.

    Os arquivos do GitHub Spec Kit deverão aparecer junto ao código existente do aplicativo. Talvez seja necessário atualizar a página para ver as alterações mais recentes.

Agora você tem um aplicativo ContosoDashboard funcional com o GitHub Spec Kit inicializado.

## Gerar uma constituição com base nos arquivos do repositório

O GitHub Spec Kit usa um arquivo constitution.md para estabelecer os princípios e as restrições que orientam todas as decisões de desenvolvimento do projeto ContosoDashboard. Ele registra as políticas organizacionais, os padrões técnicos, os requisitos de segurança e as práticas de desenvolvimento que devem ser seguidas durante a implementação.

Nesta tarefa, você usará o comando `/speckit.constitution` do GitHub Copilot para gerar uma constituição abrangente com base nos requisitos das partes interessadas da Contoso e nos arquivos existentes do projeto.

Siga estas etapas para concluir a tarefa:

1. Use o modo de exibição Explorer do Visual Studio Code para expandir as pastas **.github/agents** e **.specify/memory**.

    Essas pastas contêm os recursos do GitHub Spec Kit usados para criar o arquivo constitution.md. É útil familiarizar-se com esses arquivos antes de trabalhar na constituição.

1. Na pasta **.github/agents**, abra o arquivo **speckit.constitution.agent.md**.

1. Reserve um minuto para revisar o arquivo **speckit.constitution.agent.md**.

    Observe as instruções detalhadas fornecidas nesse arquivo Markdown. O GitHub Copilot usa essas instruções para gerar o arquivo constitution.md. O agente segue uma abordagem sistemática para criar uma constituição que registre os principais princípios e restrições.

1. Na pasta **.specify/memory**, abra o arquivo **constitution.md**.

    A versão inicial do arquivo constitution.md contém o modelo padrão de uma constituição.

1. Reserve um minuto para revisar o modelo **constitution.md**.

    Observe que o modelo vem preenchido com exemplos que ilustram princípios e restrições, incluindo segurança, desempenho, qualidade, padrões técnicos e outros.

    Você pode deixar o arquivo da constituição aberto.

1. Verifique se o modo de exibição Chat está aberto e inicie uma nova sessão.

    Para iniciar uma nova sessão, selecione o botão **New Chat** (ícone **+** na parte superior do painel Chat). Uma nova sessão garante um contexto limpo.

1. No modo Chat, insira o comando a seguir para iniciar o fluxo de trabalho da constituição:

    ```plaintext
    /speckit.constitution
    ```

    O GitHub Spec Kit oferece suporte a projetos "greenfield" e "brownfield". Os requisitos preliminares são mais importantes em projetos greenfield, pois não existe uma base de código. Neste exercício, o ContosoDashboard é um projeto brownfield com uma base de código existente; por isso, o agente analisa os arquivos atuais do projeto para gerar a constituição.

1. Acompanhe a resposta do GitHub Copilot.

    O GitHub Copilot usa o modo Chat para comunicar o progresso enquanto atualiza o arquivo constitution.md.

    O GitHub Copilot pode levar um ou dois minutos para analisar os requisitos do projeto e criar o documento da constituição. Se o fluxo de trabalho atualizar os modelos de outros arquivos do GitHub Spec Kit (spec.md, plan.md, tasks.md), você poderá aceitar as atualizações sem revisar as alterações. Esses arquivos serão gerados em tarefas posteriores.

    > **OBSERVAÇÃO**: Se o GitHub Copilot informar que não consegue acessar ou editar arquivos, abra **Settings** do Visual Studio Code, expanda **Features**, selecione **Chat** e verifique se **Chat > Agent** está habilitado.

1. Revise o arquivo constitution.md atualizado no editor.

    Prática recomendada: sempre revise as sugestões criadas por um agente.

    Depois que o GitHub Copilot atualizar a constituição, revise o documento para garantir que os requisitos foram registrados corretamente. Essa etapa é importante em um ambiente de produção, no qual a constituição representa os requisitos de negócios e a governança técnica. Neste exercício de treinamento, a revisão serve principalmente para familiarizar você com o conteúdo da constituição.

    Observe que o GitHub Copilot reconhece os princípios fundamentais do projeto ContosoDashboard e os incorpora à constituição. A constituição estabelece uma abordagem de desenvolvimento orientado por especificações e reconhece a diferença entre um aplicativo de treinamento e código de produção.

    Cada princípio deve ser claro e aplicável. Por exemplo:

    - ❌ Vago: "Use boas práticas de segurança" é muito genérico.
    - ✅ Claro: "Todos os endpoints de API devem validar tokens de autenticação e aplicar permissões baseadas em funções" é específico e aplicável.

    Se algum requisito crítico estiver ausente ou pouco claro, você poderá editar diretamente o arquivo constitution.md para adicionar ou modificar princípios.

1. Verifique se o documento da constituição está completo e aceite as alterações.

    Em um projeto real, é importante revisar a constituição com base nos seguintes critérios antes de salvá-la:

    - Completude: todas as áreas principais (segurança, desempenho, qualidade e padrões técnicos) estão contempladas.
    - Clareza: cada princípio é específico e inequívoco.
    - Consistência: os princípios não se contradizem.
    - Relevância: todos os princípios se aplicam ao projeto ContosoDashboard.

1. Salve e feche o arquivo **constitution.md**.

1. Confirme e envie os arquivos atualizados para o repositório Git.

    Por exemplo, se o arquivo constitution.md for o único atualizado, você poderá usar os seguintes comandos no terminal:

    ```powershell
    git add constitution.md
    git commit -m "Add project constitution with development principles and constraints"
    git push
    ```

    Você pode verificar o commit acessando o repositório do GitHub no navegador. O arquivo constitution.md deverá aparecer com a mensagem do commit.

A constituição funciona como um "contrato" entre os requisitos de negócios e a implementação técnica, garantindo consistência em todo o processo de desenvolvimento orientado por especificações. Ao usar o GitHub Spec Kit para gerar a especificação, o plano e as tarefas, esses princípios são consultados para garantir que a implementação esteja alinhada aos requisitos definidos.

## Criar a especificação do recurso com os requisitos das partes interessadas e a constituição

A especificação (spec.md) define o que será criado sob a perspectiva do usuário. Ela descreve recursos, histórias de usuário, critérios de aceitação e requisitos de negócios sem determinar como implementá-los. Uma especificação bem escrita serve de base para criar o plano de implementação e as tarefas.

Nesta tarefa, você usará o comando `/speckit.specify` do GitHub Copilot para gerar uma especificação detalhada do "recurso de upload e gerenciamento de documentos" com base nos requisitos fornecidos pelas partes interessadas de negócios da Contoso.

Siga estas etapas para concluir a tarefa:

1. No modo Explorer do Visual Studio Code, abra o arquivo **speckit.specify.agent.md** dentro da pasta **.github/agents**.

1. Reserve um minuto para revisar o arquivo **speckit.specify.agent.md**.

    Observe as instruções detalhadas fornecidas ao GitHub Copilot. O agente segue uma abordagem sistemática para gerar um arquivo de especificação que define claramente os requisitos.

1. No modo Explorer do Visual Studio Code, expanda a pasta **StakeholderDocs** e abra o arquivo **document-upload-and-management-feature.md**.

1. Reserve alguns minutos para ler o arquivo **document-upload-and-management-feature.md**.

    O arquivo document-upload-and-management-feature.md contém requisitos detalhados das partes interessadas para o recurso que será adicionado ao aplicativo ContosoDashboard. Requisitos claros e detalhados são essenciais para criar uma especificação eficaz.

    O documento explica que os funcionários da Contoso Corporation precisam de um recurso para carregar, organizar e compartilhar documentos de trabalho no aplicativo ContosoDashboard. O recurso deve resolver os problemas atuais de armazenamento disperso em vários locais, oferecendo uma solução centralizada e segura de gerenciamento de documentos, com controle de acesso baseado em funções. O documento também indica que o recurso deve funcionar offline para fins de treinamento, mantendo abstrações bem definidas para uma futura migração para a nuvem do Azure. A especificação define seis áreas principais de requisitos (recursos de upload, organização e navegação, gerenciamento de acesso, integração com recursos existentes, requisitos de desempenho e relatórios), além de restrições técnicas detalhadas para garantir que a implementação siga um padrão de arquitetura offline-first com abstrações baseadas em interfaces para implantação em produção. Também são fornecidas metas de desempenho e métricas de sucesso para garantir que o recurso atenda às necessidades dos usuários e aos objetivos de negócios.

    O ideal é preparar uma descrição abrangente do recurso com antecedência. No entanto, se você não tiver um documento detalhado de requisitos como o da pasta StakeholderDocs, poderá usar uma descrição mais curta que destaque os principais recursos, restrições e critérios de sucesso. Por exemplo, a descrição simplificada a seguir poderia ser usada para o recurso de upload e gerenciamento de documentos:

    ```plaintext
    Recurso: Upload e gerenciamento de documentos para o ContosoDashboard
    
    Permita que os funcionários carreguem documentos de trabalho (PDF, Office, imagens e texto), organizem-nos por categoria/projeto, compartilhem-nos com os membros da equipe e pesquisem com eficiência. O recurso deve integrar-se aos recursos existentes do painel e manter a segurança.
    
    Usuários-alvo: todos os 5.000 funcionários da Contoso, com acesso baseado em funções (funcionário, líder de equipe, gerente de projeto e administrador).
    
    Recursos principais:
    1. Upload: vários arquivos, máximo de 25 MB cada, tipos compatíveis (PDF, documentos do Office, imagens e texto), metadados (título, categoria, descrição, projeto e marcas), indicador de progresso e verificação de vírus.
    2. Organização: modos de exibição Meus documentos e Documentos do projeto; pesquisa por título/descrição/marcas/remetente/projeto (resultados em menos de 2 segundos).
    3. Gerenciamento: baixar, visualizar no navegador (PDF/imagens), editar metadados, substituir arquivos, excluir documentos e compartilhar com notificações.
    4. Integração: anexar a tarefas, widget Documentos recentes no painel e notificações sobre compartilhamentos/novos documentos do projeto.
    5. Desempenho: upload em 30 s (arquivos de 25 MB), carregamento da lista em 2 s (500 documentos), pesquisa em 2 s e visualização prévia em 3 s.
    6. Auditoria: registrar todos os uploads/downloads/exclusões/compartilhamentos e gerar relatórios administrativos.
    
    Segurança: criptografia em repouso no Azure Blob Storage, TLS 1.3 durante a transmissão, aplicação do RBAC e verificação de vírus.
    
    Critérios de sucesso: adoção de 70% em 3 meses, localização de documentos em menos de 30 s, 90% devidamente categorizados e nenhum incidente de segurança.
    
    Restrições: Azure Blob Storage, integração com ASP.NET Core, prazo de 8 a 10 semanas e autenticação do Entra ID.
    
    Fora do escopo: histórico de versões, cotas de armazenamento, exclusão reversível/lixeira, edição colaborativa, integrações externas e aplicativos móveis.
    ```

1. Verifique se o modo de exibição Chat está aberto.

    Observe que o GitHub Copilot mantém o contexto das interações anteriores na sessão de chat atual. Se você gerou o arquivo constitution.md nesta sessão, o GitHub Copilot mostrará o botão **Build Specification** próximo à parte inferior do modo Chat, que pode ser usado para iniciar a geração da especificação. Neste caso, você fornecerá explicitamente o documento de requisitos, portanto não usará esse botão.

1. No modo Chat, insira o comando a seguir para iniciar um fluxo specify que gera uma especificação a partir do documento das partes interessadas:

    ```plaintext
    /speckit.specify --file StakeholderDocs/document-upload-and-management-feature.md
    ```

    Se você não especificar um documento de requisitos usando a opção `--file`, será solicitado que descreva o recurso que deseja criar.

1. Acompanhe a resposta do GitHub Copilot e forneça ajuda quando necessário.

    > **IMPORTANTE**: O GitHub Copilot poderá solicitar ajuda durante a geração do arquivo spec.md. Por exemplo, poderá pedir permissão para criar uma ramificação do repositório para o novo recurso. Quando necessário, conceda a permissão respondendo no modo Chat.

    A criação e validação do arquivo spec.md pode levar de 4 a 6 minutos.

1. Quando o fluxo specify terminar, use o modo Explorer do Visual Studio Code para expandir as pastas **specs** e **checklists**.

1. No modo Explorer, selecione **spec.md** e reserve alguns minutos para revisar o arquivo.

    O arquivo spec.md deverá conter uma especificação detalhada do recurso de upload e gerenciamento de documentos, baseada nos requisitos das partes interessadas.

    A especificação deve ser clara, abrangente e bem estruturada. Ela deve fornecer uma base sólida para criar o plano técnico e as tarefas.

    O arquivo spec.md é baseado no modelo localizado em **.specify/templates/spec-template.md**. O arquivo spec.md atualizado deverá incluir uma especificação detalhada do 'recurso de upload e gerenciamento de documentos', com base nos requisitos fornecidos pelas partes interessadas.

    Verifique se o arquivo spec.md contém as seções obrigatórias definidas no modelo. Por exemplo:

    - **User Scenarios & Testing**: descrições dos recursos sob a perspectiva do usuário e de como testá-los.
    - **Requirements**: requisitos detalhados que devem ser atendidos, organizados por categoria.
    - **Success Criteria**: resultados mensuráveis, pressupostos e itens fora do escopo.

1. Revise o arquivo **spec.md** e verifique se os principais requisitos do documento das partes interessadas foram registrados na seção Requirements.

    Por exemplo, você deverá encontrar requisitos relacionados a:

    - Limites de tamanho de arquivo (25 MB por arquivo).
    - Tipos de arquivo compatíveis (PDF, documentos do Office, imagens e arquivos de texto).
    - Metas de desempenho (carregamento de páginas em 2 segundos, uploads em 30 segundos).

1. Revise o arquivo **spec.md** e verifique se os cenários de aceitação (associados aos cenários de usuário) são específicos e testáveis.

    Os cenários de aceitação devem seguir o formato **Dado-Quando-Então**. Eles devem definir claramente as condições de sucesso ou falha. Por exemplo:

    - ✅ Bom: **Dado** que entrei como funcionário, **Quando** acesso a página de documentos e seleciono um arquivo PDF com menos de 25 MB e metadados válidos (título e categoria), **Então** o documento é carregado com sucesso e aparece na lista "Meus documentos", com todos os metadados exibidos corretamente.

    - ✅ Bom: **Dado** que um funcionário tenta carregar um arquivo de 30 MB, **Quando** a validação é executada, **Então** ele vê uma mensagem de erro informando o limite de 25 MB.

    - ❌ Evite: critérios vagos como "O upload deve funcionar bem" ou "O sistema deve ser rápido".

1. No modo Explorer, selecione **requirements.md** e reserve um minuto para revisar o arquivo.

    Verifique se não há problemas relatados no arquivo **requirements.md**. Todos os itens da lista de verificação deverão estar aprovados.

1. Aceite as atualizações de arquivo sugeridas e salve os arquivos **spec.md** e **requirements.md**.

1. Confirme os arquivos da especificação e publique a nova ramificação no repositório Git.

    Por exemplo:

    Abra o modo SOURCE CONTROL do Visual Studio Code, prepare as alterações, insira uma mensagem de commit como "Adicionar especificação do recurso de upload e gerenciamento de documentos" e publique a nova ramificação no repositório Git.

A especificação define o "que" será feito, sem determinar o "como". Ela não especifica linguagens de programação, estruturas, esquemas de banco de dados ou organização do código; esses detalhes de implementação são definidos nas etapas Plan e Tasks com base nas restrições técnicas da constituição. A especificação se concentra nas necessidades dos usuários e nos requisitos de negócios, facilitando a revisão por partes interessadas não técnicas.

## Atualizar a especificação com requisitos esclarecidos

O comando `/speckit.clarify` ajuda a identificar ambiguidades, lacunas e áreas insuficientemente especificadas. O GitHub Copilot analisa a especificação e faz perguntas direcionadas para garantir que todos os requisitos estejam claros e completos antes de avançar para o planejamento técnico.

Nesta tarefa, você usará o processo de esclarecimento para refinar a especificação de upload e gerenciamento de documentos.

Siga estas etapas para concluir a tarefa:

1. Verifique se o modo Chat do GitHub Copilot está aberto.

1. No modo Chat, insira o comando a seguir para iniciar o processo de esclarecimento:

    ```plaintext
    /speckit.clarify
    ```

1. Acompanhe a resposta do GitHub Copilot e forneça ajuda quando necessário.

    O GitHub Copilot analisa o arquivo spec.md e avalia se são necessárias perguntas de esclarecimento.

    Por exemplo, você poderá receber perguntas semelhantes às seguintes:

    - "Quando um usuário é removido de um projeto depois de carregar documentos para esse projeto, o que deve acontecer com esses documentos?"
    - "Quando um projeto é excluído, o que deve acontecer com todos os documentos associados a ele?"
    - "Quando o proprietário exclui um documento compartilhado, o que acontece com os destinatários que tinham acesso a ele?"
    - "Quando um usuário carrega um documento cujo nome contém caracteres especiais (por exemplo, Q4 Report (2025) - Finance & Ops.pdf), como o sistema deve tratá-lo?"
    - "Como o sistema deve responder se o espaço em disco acabar durante o upload de um documento?"

    As perguntas são apresentadas uma por vez.

1. Se forem necessários esclarecimentos, avalie cada pergunta antes de responder.

    Em um ambiente de produção, suas respostas devem refletir uma análise cuidadosa das necessidades de negócios, da experiência do usuário e das restrições técnicas. Para este treinamento, porém, você pode selecionar a opção recomendada para cada pergunta.

    Quando você responder, o GitHub Copilot atualizará o arquivo spec.md com os esclarecimentos.

    > **OBSERVAÇÃO**: Se o GitHub Copilot apresentar outras rodadas de perguntas, continue respondendo até que ele indique que não são necessários mais esclarecimentos. Normalmente, o processo inclui de 1 a 2 rodadas enquanto o GitHub Copilot refina a especificação.

    Quando o processo de esclarecimento terminar, revise o arquivo **spec.md** atualizado e aceite as alterações.

    - Confira se suas respostas foram refletidas corretamente na especificação.
    - Verifique se as áreas antes ambíguas agora têm requisitos claros.
    - Procure novos critérios de aceitação adicionados com base nos esclarecimentos.

    Se necessário, você poderá fazer edições manualmente. Por exemplo, se o GitHub Copilot interpretar uma resposta de forma diferente da pretendida, edite a especificação diretamente para corrigi-la.

1. Se o processo de esclarecimento tiver resultado em alterações, salve o arquivo **spec.md** atualizado e confirme e sincronize as alterações.

É importante garantir que a especificação forneça orientações claras e abrangentes. Ao resolver as ambiguidades desde o início, você reduz o risco de criar a solução errada ou precisar fazer alterações significativas mais adiante no desenvolvimento.

## Gerar o plano técnico usando a especificação e a constituição

O plano técnico faz a ligação entre o "que" (especificação) e o "como" (implementação). Ele define a arquitetura, as escolhas tecnológicas, os modelos de dados, os designs de API e a abordagem de implementação, respeitando as restrições estabelecidas na constituição.

Nesta tarefa, você usará o comando `/speckit.plan` do GitHub Copilot para gerar um plano técnico de implementação abrangente.

Siga estas etapas para concluir a tarefa:

1. No modo Explorer do Visual Studio Code, abra o arquivo **speckit.plan.agent.md** dentro da pasta **.github/agents**.

1. Reserve um minuto para revisar o arquivo **speckit.plan.agent.md**.

    Observe as instruções detalhadas fornecidas ao GitHub Copilot. O agente segue uma abordagem sistemática para gerar um arquivo de plano que descreve a estratégia de implementação técnica.

    Se tiver interesse, você também poderá revisar o arquivo **.specify/templates/plan-template.md** para conhecer a estrutura usada no arquivo plan.md.

1. Verifique se o modo Chat do GitHub Copilot está aberto.

1. No modo Chat, insira o comando a seguir para iniciar o planejamento técnico:

    ```dotnetcli
    /speckit.plan
    ```

1. Acompanhe a resposta do GitHub Copilot e forneça ajuda no modo Chat.

    O GitHub Copilot analisa os arquivos constitution.md e spec.md para gerar o plano. Conceda permissões e forneça ajuda quando necessário.

    O GitHub Copilot pode levar de 6 a 8 minutos para gerar o plano técnico e os arquivos Markdown associados.

1. Quando o fluxo de trabalho do plano terminar, verifique se os seguintes arquivos estão na pasta **specs**:

    - **plan.md**
    - **research.md**
    - **quickstart.md**
    - **data-model.md**

    Também poderá haver um ou mais arquivos em uma pasta **contracts**.

1. Reserve alguns minutos para revisar os arquivos **research.md**, **plan.md**, **quickstart.md** e **data-model.md**.

    - O arquivo research.md registra os resultados da pesquisa e as decisões tecnológicas para o recurso de upload e gerenciamento de documentos.
    - O arquivo plan.md descreve o plano técnico de implementação do recurso.
    - O arquivo quickstart.md fornece instruções de configuração e uma visão geral de como começar a implementação.
    - O arquivo data-model.md define as entidades de dados, propriedades e relações necessárias ao recurso.

    > **OBSERVAÇÃO**: Em um cenário de produção, verifique se o plano oferece uma descrição abrangente do contexto técnico e uma estratégia de implementação claramente definida para o novo recurso. Os arquivos de pesquisa, início rápido e modelo de dados devem complementar o plano com contexto e detalhes adicionais. Neste exercício, concentre-se em conhecer o conteúdo de cada arquivo.

1. Depois de revisar os arquivos, aceite as atualizações.

    Se o plano omitir detalhes importantes ou incluir pressupostos dos quais você discorda, poderá:

    - Editar diretamente o arquivo plan.md; ou
    - Fazer perguntas complementares no Chat do GitHub Copilot. Por exemplo:

    ```plaintext
    O plano deve incluir um trabalho em segundo plano para processar verificações de vírus. Adicione detalhes sobre o uso do Azure Functions com gatilhos do Queue Storage para realizar a verificação assíncrona dos arquivos após o upload.
    ```

1. Salve os arquivos e confirme e sincronize as alterações.

O plano técnico agora serve como roteiro para a implementação. Ele traduz os requisitos de negócios em decisões técnicas concretas, respeitando as restrições organizacionais.

## Gerar o arquivo de tarefas usando a especificação, o plano e a constituição

O arquivo tasks.md divide o plano técnico em etapas de implementação específicas e aplicáveis. Cada tarefa deve ser pequena o suficiente para ser concluída em um prazo razoável (normalmente, de algumas horas a um dia sem assistência de IA) e ter critérios de aceitação claros.

Nesta tarefa, você usará o comando `/speckit.tasks` do GitHub Spec Kit para gerar uma lista abrangente de tarefas e um plano de implementação dividido em fases.

Siga estas etapas para concluir a tarefa:

1. No modo Explorer do Visual Studio Code, abra o arquivo **speckit.tasks.agent.md** dentro da pasta **.github/agents**.

1. Reserve um minuto para revisar o arquivo **speckit.tasks.agent.md**.

    Observe as instruções detalhadas fornecidas ao GitHub Copilot. O agente segue uma abordagem sistemática para gerar um arquivo tasks.md que divide o plano de implementação em tarefas gerenciáveis.

1. Verifique se o modo Chat do GitHub Copilot está aberto.

1. No modo Chat, insira o comando a seguir para começar a gerar o arquivo tasks.md:

    ```dotnetcli
    /speckit.tasks
    ```

1. Acompanhe a resposta do GitHub Copilot e forneça ajuda no modo Chat.

    O GitHub Copilot analisa os arquivos spec.md e plan.md e gera tarefas no arquivo tasks.md.

    O GitHub Copilot pode levar de 3 a 4 minutos para gerar o arquivo tasks.md. Conceda permissões e forneça ajuda quando necessário.

1. Quando o fluxo de tarefas terminar, reserve alguns minutos para revisar o arquivo **tasks.md**.

    O arquivo tasks.md deverá apresentar uma lista de tarefas organizada por fase e história de usuário.

    Verifique se as tarefas abrangem os requisitos da especificação e do plano. Por exemplo:

    - Cada requisito funcional deverá corresponder a uma ou mais tarefas.
    - Os requisitos de segurança deverão ter tarefas de implementação correspondentes.
    - Os requisitos de desempenho deverão ter tarefas de teste.
    - Os pontos de integração deverão ter tarefas próprias.

    Verifique se as tarefas estão ordenadas de forma lógica. Por exemplo:

    - As tarefas de fundação (banco de dados, modelos) vêm primeiro.
    - As tarefas de API de back-end se baseiam na fundação.
    - As tarefas de front-end fazem referência aos endpoints de back-end.
    - As tarefas de teste vêm após a implementação.
    - As tarefas de implantação ficam por último.

1. Verifique se cada tarefa é específica e aplicável:

    - ✅ Bom: "Criar a entidade Document com as propriedades: DocumentId, Title, Description, FileName, FileSize, BlobStorageUrl".
    - ❌ Vago: "Configurar coisas do banco de dados".

    Verifique se o escopo das tarefas é razoável:

    - Os desenvolvedores devem conseguir concluir cada tarefa em algumas horas ou em um dia.
    - Se uma tarefa parecer grande demais, talvez seja necessário dividi-la durante a implementação.

    Se necessário, você poderá adicionar dependências ou observações às tarefas. Por exemplo:

    ```markdown
    - [ ] Tarefa 12: implementar o endpoint POST /api/documents do DocumentController
    - Depende de: Tarefa 11 (DocumentService)
    - Observação: inclua tratamento abrangente de erros para limites de tamanho e tipos de arquivo não compatíveis.
    ```

1. Aceite as atualizações sugeridas e salve o arquivo **tasks.md**.

1. Confirme as alterações e sincronize as atualizações.

O arquivo tasks.md agora fornece um roteiro claro para a implementação.

## Implementar as tarefas necessárias para um aplicativo MVP

Com a especificação, o plano técnico e o documento de tarefas definidos, você está pronto para implementar o recurso de upload e gerenciamento de documentos. O fluxo implement demonstra como o desenvolvimento orientado por especificações guia a implementação e como o GitHub Copilot auxilia na geração de código com base no contexto estabelecido.

Nesta tarefa, você revisará a estratégia de implementação e usará `speckit/implement` para implementar a versão MVP do aplicativo.

Siga estas etapas para concluir a tarefa:

1. Abra o arquivo **tasks.md**, localize a seção **Implementation Strategy** e revise a estratégia sugerida de "MVP primeiro".

A estratégia "MVP primeiro" busca entregar um recurso funcional o mais rápido possível. Ela deve priorizar a conclusão das fases críticas e bloqueadoras para estabelecer uma base funcional antes de desenvolver a primeira história de usuário (US1).

    Por exemplo, a estratégia de implementação do MVP poderá ser semelhante a este exemplo:

    ```plaintext
    **Fases**: Configuração → Fundação → somente US1  
    **Tarefas**: T001 - T045 (45 tarefas)  
    **Tempo estimado**: de 6 a 8 horas para um desenvolvedor familiarizado com ASP.NET Core/Blazor  
    **Entrega**: os usuários podem carregar e visualizar seus documentos
    ```

1. No modo Chat, insira um comando para iniciar o fluxo implement usando a estratégia "MVP primeiro":

    Crie um comando que especifique o intervalo de tarefas necessário para implementar a versão MVP do recurso. Use o intervalo indicado na seção Implementation Strategy do arquivo tasks.md.

    > **IMPORTANTE**: o comando inserido deve fazer referência ao intervalo específico de tarefas definido no arquivo tasks.md.

    Por exemplo, com base no exemplo de implementação do MVP da etapa anterior, você poderá inserir o seguinte comando:

    ```dotnetcli
    /speckit.implement Implemente a estratégia MVP primeiro (Tarefas: T001 - T045)
    ```

    Esse comando instrui o GitHub Copilot a começar a implementar as tarefas necessárias para a versão MVP do recurso de upload e gerenciamento de documentos.

1. Acompanhe a resposta do GitHub Copilot e forneça ajuda no modo Chat.

    O agente desenvolve o recurso de forma incremental, tarefa por tarefa, seguindo a ordem definida no arquivo tasks.md.

    > **OBSERVAÇÃO**: O GitHub Copilot verifica cuidadosamente o próprio trabalho durante a implementação. Ele também mantém você envolvido no processo, solicitando ajuda com frequência. O tempo necessário para concluir a implementação pode depender da rapidez com que você responde a essas solicitações.

1. Se for necessário realizar testes manuais para verificar uma tarefa, siga as etapas descritas no modo Chat e informe os resultados ao GitHub Copilot.

    Você poderá encontrar problemas durante os testes manuais. Por exemplo:

    1. O GitHub Copilot informa que é necessário testar manualmente se os uploads de arquivos estão funcionando corretamente.
    1. O aplicativo já está em execução localmente. O modo Chat fornece a URL para abrir no navegador (por exemplo, `http://localhost:5000`).
    1. Você abre o aplicativo no navegador, entra como Ni Kang e acessa a página Meus documentos.
    1. O aplicativo parece não responder e exibe a mensagem "Carregando documentos..." na interface.
    1. Você seleciona o botão Upload Document, mas nada acontece.
    1. Você tenta sair, mas o aplicativo continua sem responder. Nenhum botão funciona.

    Nesse momento, você precisa informar o problema ao GitHub Copilot:

    1. Volte ao modo Chat do Visual Studio Code.
    1. Informe o problema no modo Chat. Por exemplo:

        ```plaintext
        Abri o aplicativo no navegador em http://localhost:5000. Consegui entrar como Ni Kang e acessar a página Meus documentos. No entanto, encontrei um problema: o aplicativo parece não responder e exibe a mensagem "Carregando documentos..." na interface. Quando seleciono o botão Upload Document, nada acontece. Também tentei sair, mas o aplicativo continua sem responder e nenhum botão funciona. Você pode me ajudar a solucionar esse problema?
        ```

    Ao informar um problema, o GitHub Copilot usa os dados fornecidos para iniciar a depuração. Uma descrição detalhada, incluindo o que está funcionando, ajuda o GitHub Copilot a entender melhor o problema. Para resolver alguns casos, ele poderá precisar de mais detalhes, como mensagens de erro específicas. Forneça as informações adicionais solicitadas para ajudar a diagnosticar e resolver o problema.

    Continue fornecendo ajuda até que o problema seja resolvido. Em seguida, o GitHub Copilot deverá pedir que você retome os testes manuais.

1. Continue o fluxo implement até concluir todas as tarefas necessárias para o aplicativo MVP.

    O GitHub Copilot deverá avisar no modo Chat quando a implementação do MVP estiver concluída.

1. Revise e aceite todas as alterações feitas nos arquivos do projeto.

    Neste exercício de laboratório, você pode aceitar todas as alterações feitas pelo GitHub Copilot sem uma revisão detalhada. Em um ambiente de produção, porém, é importante revisar cuidadosamente todas as alterações de código para garantir que atendam aos padrões de qualidade e aos requisitos do projeto.

1. Reserve alguns minutos para verificar os cenários de aceitação do aplicativo MVP.

    Você encontrará os cenários de aceitação no arquivo spec.md, na seção **User Scenarios & Testing**. O aplicativo MVP geralmente está associado à primeira história de usuário (US1) desse arquivo.

    Você também pode pedir ao GitHub Copilot as etapas necessárias para testar manualmente a implementação do MVP. Por exemplo, insira este prompt no modo Chat:

    ```plaintext
    Você pode fornecer as etapas necessárias para testar manualmente a implementação do MVP?
    ```

    Use o Visual Studio Code para executar o aplicativo e teste manualmente as funcionalidades de upload e gerenciamento de documentos para garantir que funcionem como esperado.

    Por exemplo, você pode seguir estas etapas para testar manualmente o upload de documentos:

    1. Acesse http://localhost:5000.
    1. Entre como Ni Kang (Employee).
    1. Selecione **Documents** no menu de navegação.
    1. Use a interface fornecida para abrir a janela de seleção de arquivos.
    1. Localize e selecione um arquivo PDF com menos de 25 MB e preencha os campos Title ("Documento de teste") e Category ("Arquivos pessoais").
    1. Selecione a opção "Upload" para iniciar o processo.
    1. Verifique se aparece um indicador de progresso do upload.
    1. Verifique se o documento aparece na lista de documentos carregados.

1. Informe ao GitHub Copilot os resultados dos testes manuais.

    Por exemplo:

    - Se o teste tiver sido bem-sucedido, você poderá continuar para o próximo teste ou enviar um relatório semelhante a este:

        "Abri o aplicativo no navegador em http://localhost:5000. Consegui entrar como Ni Kang e acessar a página Meus documentos. Posso carregar um arquivo PDF com menos de 25 MB usando o título 'Documento de teste' e a categoria 'Arquivos pessoais'. O indicador de progresso apareceu e o documento está na lista de documentos carregados. A tarefa T041 foi concluída com sucesso."

    - Se a tarefa tiver falhado, informe o problema ao GitHub Copilot para receber ajuda.

        Por exemplo: "Abri o aplicativo no navegador em http://localhost:5000. Consegui entrar como Ni Kang e acessar a página Meus documentos. Posso selecionar um documento e preencher os campos Title e Category, mas ocorre um erro quando tento carregá-lo. Vejo um indicador de progresso na página Upload Document; no entanto, a página Meus documentos não reconhece o documento carregado. Você pode me ajudar a resolver o problema?"

    O GitHub Copilot pode ajudar a diagnosticar e corrigir problemas, implementar melhorias na interface do usuário ou sugerir as próximas etapas.

1. Continue os testes manuais e informe os resultados até que todos os cenários de aceitação do aplicativo MVP sejam aprovados.

1. Depois de testar o aplicativo MVP com sucesso, confirme e sincronize os arquivos da implementação.

> **OBSERVAÇÃO**: Se houver tempo, você poderá continuar implementando tarefas além do escopo do MVP. Instrua o GitHub Copilot a prosseguir com o próximo conjunto de tarefas ou selecione manualmente tarefas específicas para implementar em seguida.

Principais observações:

- O GitHub Copilot gera código alinhado à especificação porque consulta os arquivos *spec.md*, *plan.md* e *tasks.md* do espaço de trabalho.
- Comentários detalhados baseados nos requisitos da especificação orientam o GitHub Copilot para produzir implementações precisas.
- A abordagem de desenvolvimento orientado por especificações ajuda a não esquecer requisitos (limites de tamanho de arquivo, tipos compatíveis etc.), pois eles estão documentados explicitamente.
- Critérios de aceitação claros facilitam a verificação de que a implementação atende aos requisitos.

Em uma implementação completa, você continuaria com todas as tarefas restantes do arquivo tasks.md, usando uma abordagem por fases para desenvolver sistematicamente o recurso completo. O desenvolvimento orientado por especificações mantém o foco nos requisitos e evita o aumento descontrolado do escopo ou a falta de funcionalidades.

## Limpeza

Agora que concluiu o exercício, reserve um minuto para verificar se fez alterações na conta do GitHub ou na assinatura do GitHub Copilot que não deseja manter. Por exemplo, talvez queira excluir o repositório ContosoDashboard. Se estiver usando um computador local como ambiente de laboratório, confira se deseja manter as ferramentas instaladas durante o exercício. Você pode arquivar ou excluir o clone local do repositório criado para este exercício.