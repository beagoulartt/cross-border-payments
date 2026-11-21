# Cross-Border Payments

Demo de portfólio em React, ASP.NET Core e SQL Server. O painel consulta as taxas de referência do Banco do Canadá. Os pagamentos do modo portfólio usam beneficiários fictícios e são simulados.

## Teste local

Na pasta `CrossBorderPayments.Api`, inicie o SQL Server LocalDB e aplique as migrações. O modo portfólio vem ativado no ambiente de desenvolvimento.

```powershell
sqllocaldb start MSSQLLocalDB
dotnet ef database update
dotnet run
```

Em outro terminal, abra `cross-border-payments-client` e rode `npm ci` e `npm run dev`. Acesse [o frontend local](http://localhost:5173).

Entre com **Try as user**. Gere uma cotação CAD → BRL e crie um pagamento para o beneficiário fictício em BRL. Use **Switch to administrator**, clique em **Request approval** e depois em **Approve**. O pagamento passa para **Processing** e aparece como **Completed** quando o painel consulta o status após a simulação. Use **Switch to user** para acompanhá-lo no Dashboard. Use dados fictícios durante a demonstração.

## Publicação no Azure

1. Crie um Azure App Service com suporte a .NET 10 e uma instância separada de Azure SQL para a demo. Ative **HTTPS Only** no App Service. Se usar a oferta gratuita do Azure SQL, escolha pausar ao atingir a cota mensal.
2. Configure `Portfolio__Enabled=true` e `ConnectionStrings__DefaultConnection` nas configurações do App Service. Guarde a conexão nas configurações do ambiente.
3. Aplique as migrações ao banco do Azure antes de liberar o site. Para usar o EF CLI, defina `ConnectionStrings__DefaultConnection` temporariamente no terminal apontando para o banco da demo e rode `dotnet ef database update` na pasta da API.
4. Rode `dotnet publish -c Release -o bin/portfolio-publish`. O comando compila o React e empacota os arquivos estáticos junto com a API. Para usar ZIP Deploy, compacte o **conteúdo** da pasta de publicação e envie o ZIP ao App Service.
5. Abra a URL HTTPS do App Service em uma janela anônima. Teste a criação e aprovação de um pagamento e confirme em uma segunda janela com outra sessão que o pagamento fica visível só na sessão original.

O modo portfólio usa a mesma origem para o React e a API. A troca de perfil mantém a área isolada do visitante. Sair da sessão ou deixar a sessão expirar inicia uma nova área na próxima visita. As cotações pendentes ficam em memória e precisam ser refeitas após um reinício da API.
