OrionHealth: Servidor HL7 MLLP/TLS para Integração de Dados de Saúde 

🏥Bem-vindo ao repositório do OrionHealth, um servidor robusto para integração de dados de saúde. Ele recebe e processa mensagens HL7 (Health Level Seven) via protocolo MLLP/TLS, persistindo informações de pacientes e resultados de exames em um banco de dados.Este projeto demonstra as melhores práticas de desenvolvimento, automação de CI/CD e gerenciamento de contêineres, simulando um ambiente de integração de sistemas de saúde real.

📋 Tabela de Conteúdos
  Visão Geral
  Tecnologias
  FuncionalidadesCI/CD (GitHub Actions)
  Como Rodar Localmente
  Autor

💡 Visão GeralO OrionHealth atua como um ponto de entrada seguro para mensagens HL7 ORU^R01. Ele faz o parsing dos dados recebidos e os armazena em um banco de dados Oracle, respondendo com mensagens de reconhecimento (ACK/NAK) conforme o padrão HL7.

🏗️ TecnologiasLinguagem:
C# (.NET 8)
Protocolo: HL7 v2.5.1 (MLLP/TLS)
Parsing HL7: NHapi
Banco de Dados: Oracle Database (via Docker)
ORM/Acesso a Dados: Entity Framework Core, Dapper
Injeção de Dependência: Microsoft.Extensions.DependencyInjection
Controle de Versão: Git (Git Flow)
Automação: GitHub Actions
Contêinerização: Docker, Docker Compose

✨ Funcionalidades
  Recebimento Seguro de Mensagens HL7: Servidor MLLP/TLS para comunicação criptografada.
  Parsing e Persistência de Dados Clínicos: Extração e armazenamento de informações de pacientes e resultados de exames.
  Respostas HL7 Padronizadas: Geração automática de ACK/NAK.
  Serviço de Background: Operação contínua como um .NET Worker Service.
  
🚀 CI/CD (GitHub Actions)
  O projeto utiliza um pipeline de CI/CD completo para garantir a qualidade e automatizar a entrega:
  Integração Contínua (CI): Acionado em Pull Requests (develop) e pushes (develop/main), realizando builds e testes automatizados.
  Entrega Contínua (CD): Acionado por push para a branch main, fazendo login seguro no Docker Hub e publicando a imagem Docker (latest e commit hash).
⚙️ Como Rodar Localmente
  Pré-requisitos: Docker Desktop, Git, OpenSSL.
  Clone o Repositório: git clone https://github.com/ElderSousa/OrionHealth.git
  
  cd OrionHealth
  
  Gere o Certificado TLS (app-server.pfx com senha 123456 na pasta certs/):cd certs
  openssl req -x509 -newkey rsa:4096 -keyout app-server.key -out app-server.crt -sha256 -days 365 -nodes -subj "/CN=app-server"
  openssl pkcs12 -export -out app-server.pfx -inkey app-server.key -in app-server.crt -passout pass:123456
  
  cd ..
  
  Inicie os Contêineres (aplicação + Oracle DB): docker-compose up --build -d
  Envie uma Mensagem HL7 de Teste: Use o OrionHealth.TestClient (test/OrionHealth.TestClient/Program.cs) para enviar para localhost:1080.
  dotnet run --project test/OrionHealth.TestClient/OrionHealth.TestClient.csproj

👤 Autor
  Elder Sousa
  GitHub
  LinkedIn: www.linkedin.com/in/elder-sousa-5ab645bb
  Docker Hub: eldersousadevelop
