OrionHealth: Servidor de Integração de Saúde com Arquitetura de Microserviços
🏥 Bem-vindo ao repositório do OrionHealth! Este projeto evoluiu para uma robusta plataforma de integração de dados de saúde, agora baseada em uma arquitetura de microserviços assíncrona.O sistema atua como um gateway seguro que recebe mensagens HL7 (Health Level Seven) via MLLP/TLS, as enfileira usando RabbitMQ para processamento desacoplado, e persiste as informações em um banco de dados Oracle. Este projeto demonstra as melhores práticas de Clean Architecture, CI/CD, orquestração de contêineres e escalabilidade.
📋 Tabela de ConteúdosArquitetura do SistemaVisão GeralTecnologiasFuncionalidadesCI/CD (GitHub Actions)Como Rodar LocalmenteAutor🏛️ Arquitetura do SistemaO OrionHealth é agora composto por microserviços independentes que se comunicam de forma assíncrona, garantindo alta performance e resiliência.Gateway (gateway): Um serviço leve e rápido, responsável por ser o único ponto de entrada. Ele recebe as mensagens HL7, as publica em uma fila do RabbitMQ e responde imediatamente com um ACK.Processador (hl7-processor): Um serviço de background que consome as mensagens da fila, faz o parsing, aplica a lógica de negócio e persiste os dados no banco de dados Oracle.Mensageria (rabbitmq): Atua como a "central de correios", desacoplando o recebimento do processamento e permitindo a escalabilidade futura do sistema.
💡 Visão GeralO OrionHealth atua como um ponto de entrada seguro para mensagens HL7, suportando agora múltiplos tipos de eventos como ORU^R01 (Resultados de Exames) e ADT^A08 (Atualização de Dados do Paciente). As mensagens são enfileiradas e processadas de forma assíncrona, com as informações sendo armazenadas em um banco de dados Oracle.
🏗️ TecnologiasLinguagem: C# (.NET 8)Arquitetura: Microserviços, Clean Architecture, DDDMensageria: RabbitMQProtocolo: HL7 v2.5.1 (MLLP/TLS)Parsing HL7: NHapiBanco de Dados: Oracle Database (via Docker)ORM/Acesso a Dados: Entity Framework Core, DapperControle de Versão: Git (Git Flow)Automação: GitHub ActionsContêinerização: Docker, Docker Compose✨ FuncionalidadesGateway de Entrada Seguro: Servidor MLLP/TLS para comunicação criptografada.Processamento Assíncrono: Uso do RabbitMQ para enfileirar mensagens, garantindo que o gateway nunca fique bloqueado.Suporte a Múltiplos Eventos HL7: Processamento de mensagens ORU^R01 e ADT^A08.Microserviços Independentes: Lógica de recebimento (gateway) e processamento (hl7-processor) totalmente desacopladas.Respostas HL7 Padronizadas: Geração automática de ACK/NAK.
🚀 CI/CD (GitHub Actions)O projeto utiliza um pipeline de CI/CD completo para garantir a qualidade e automatizar a entrega:Integração Contínua (CI): Acionado em Pull Requests e pushes, realizando builds e testes automatizados.Entrega Contínua (CD): Acionado por push para develop ou main, fazendo login seguro no Docker Hub e publicando as imagens Docker para cada microserviço.
⚙️ Como Rodar LocalmentePré-requisitos: Docker Desktop, Git.Clone o Repositório:git clone https://github.com/ElderSousa/OrionHealth.git
cd OrionHealth
Gere o Certificado TLS (se não existir):A pasta certs já contém um certificado orionhealth.pfx com a senha 123456. Se precisar gerar um novo, use os comandos openssl.
Crie o Script de Inicialização do Banco de Dados:Crie uma pasta chamada oracle-init na raiz do projeto.Dentro dela, crie um arquivo 01_init.sql com o seguinte conteúdo:
CREATE USER orionhealth IDENTIFIED BY oracle;
GRANT CONNECT, RESOURCE, DBA TO orionhealth;
GRANT UNLIMITED TABLESPACE TO orionhealth;

Inicie os Contêineres:Este comando irá construir as imagens e iniciar todos os serviços (gateway, processador, Oracle e RabbitMQ).
docker compose up --build -d
Aplique as Migrations:Aguarde um ou dois minutos para o Oracle iniciar completamente e depois execute:
dotnet ef database update --startup-project src/OrionHealth.Worker
Envie uma Mensagem HL7 de Teste:
dotnet run --project test/OrionHealth.TestClient

(Opcional) Monitore as Filas:Acesse a interface de gestão do RabbitMQ em http:/localhost:15672/ 
(login: guest/guest).

👤 AutorElder Sousa
GitHub: https://github.com/ElderSousa/OrionHealth.git
LinkedIn: https://www.linkedin.com/in/elder-sousa-5ab645bb/
Imagem no Docker Hub: https://lnkd.in/dTjJhfUe