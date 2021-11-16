using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace Program{
    class Program{
        private DocumentClient client;
        static async Task Main(String[] args){
            try{
                Program p = new Program();
                p.BasicOperations().Wait();
            } catch(DocumentClientException dce){
                 Exception baseException = dce.GetBaseException();
                 Console.WriteLine("{0} Ocorreu um Erro: {1}, Mensagem: {2}", dce.StatusCode, dce.Message, baseException.Message);   
            } catch(Exception ex){
                Exception baseException = ex.GetBaseException();
                Console.WriteLine("Erro: {0}, Mensagem: {1}", ex.Message, baseException.Message);
            } finally{
                Console.WriteLine("Fim da Demo, Pressione Qualquer Tecla para Sair. ");
                Console.ReadKey();
            }

        }

        private async Task BasicOperations(){
    
            this.client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["accountEndpoint"]), ConfigurationManager.AppSettings["accountKey"]);
            await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = "Users" });
            await this.client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri("Users"), new DocumentCollection { Id = "WebCustomers" });
            Console.WriteLine("Database and collection validation complete");
        
            User yanhe = new User{
                Id = "1",
                UserId = "yanhe",
                LastName = "He",
                FirstName = "Yan",
                Email = "yanhe@contoso.com",
                OrderHistory = new OrderHistory[]{
                    new OrderHistory {
                        OrderId = "1000",
                        DateShipped = "08/17/2018",
                        Total = "52.49"
                    }
                },
            ShippingPreference = new ShippingPreference[]{
                new ShippingPreference {
                        Priority = 1,
                        AddressLine1 = "90 W 8th St",
                        City = "New York",
                        State = "NY",
                        ZipCode = "10001",
                        Country = "USA"
                }
            },
            };

                await this.CreateUserDocumentIfNotExists("User", "WebCustomers", yanhe);

            User nelapin = new User{
                Id = "2",
                UserId = "nelapin",
                LastName = "Pindakova",
                FirstName = "Nela",
                Email = "nelapin@contoso.com",
                Dividend = "8.50",
                OrderHistory = new OrderHistory[]
                {
                    new OrderHistory {
                    OrderId = "1001",
                    DateShipped = "08/17/2018",
                    Total = "105.89"
                }
                 },
            ShippingPreference = new ShippingPreference[]{
                 new ShippingPreference {
                    Priority = 1,
                    AddressLine1 = "505 NW 5th St",
                    City = "New York",
                    State = "NY",
                    ZipCode = "10001",
                    Country = "USA"
                 },
            new ShippingPreference {
                    Priority = 2,
                    AddressLine1 = "505 NW 5th St",
                    City = "New York",
                    State = "NY",
                    ZipCode = "10001",
                    Country = "USA"
            }
            },
            Coupons = new CouponsUsed[]{
                new CouponsUsed{
                    CouponCode = "Fall2018"
                }
            }
            };

              await this.CreateUserDocumentIfNotExists("User", "WebCustomers", nelapin);
              await this.ReadUserDocument("User", "WebCustomers", yanhe);
              yanhe.LastName = "Suh";
              await this.ReplaceUserDocument("User", "WebCustomers", yanhe);
              this.ExecuteSimpleQuery("User", "WebCustomers");
              await this.RunStoredProcedure("User", "WebCustomers", yanhe);
              await this.DeleteUserDocument("User", "WebCustomers", yanhe);
        }

        private void WriteToConsoleAndPromptToContinue(string format, params object[] args){
            Console.WriteLine(format, args);
            Console.WriteLine("Press any key to continue ...");
            Console.ReadKey();
        }

        private async Task ReadUserDocument(string databaseName, string collectionName, User user){
         try{
             await this.client.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, user.Id), new RequestOptions { PartitionKey = new PartitionKey(user.UserId) });
             this.WriteToConsoleAndPromptToContinue("Read user {0}", user.Id);
            }
        catch (DocumentClientException de){
             if (de.StatusCode == HttpStatusCode.NotFound){
             this.WriteToConsoleAndPromptToContinue("User {0} not read", user.Id);
        }else{
            throw;
        }
        }
        }

        private async Task DeleteUserDocument(string databaseName, string collectionName, User deletedUser){
            try{
                await this.client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, deletedUser.Id), new RequestOptions{ PartitionKey = new PartitionKey(deletedUser.UserId)});
                Console.WriteLine("Deleted user {0}", deletedUser.Id);
            } catch (DocumentClientException dce){
                if(dce.StatusCode == HttpStatusCode.NotFound){
                    this.WriteToConsoleAndPromptToContinue("User {0} Not Found For Deletion", deletedUser.Id);
                } else{
                    throw;
                }
            }
        }

        private async Task ReplaceUserDocument(string databaseName, string collectionName, User updatedUser){
            try{
                await this.client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, updatedUser.Id), updatedUser, new RequestOptions { PartitionKey = new PartitionKey(updatedUser.UserId) });
                this.WriteToConsoleAndPromptToContinue("Replaced Last Name For {0}", updatedUser.LastName);
            } catch(DocumentClientException dce){
                if(dce.StatusCode == HttpStatusCode.NotFound){
                    this.WriteToConsoleAndPromptToContinue("User{0} Not Found For Replacement", updatedUser.Id);
                } else{
                    throw;
                }
            }
        } 
        private async Task CreateUserDocumentIfNotExists(string databaseName, string collectionName, User user){
       
        try{
            await this.client.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, user.Id), new RequestOptions { PartitionKey = new PartitionKey(user.UserId) });
            this.WriteToConsoleAndPromptToContinue("User {0} already exists in the database", user.Id);
        }
        catch (DocumentClientException de){
            if (de.StatusCode == HttpStatusCode.NotFound){
                await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), user);
                this.WriteToConsoleAndPromptToContinue("Created User {0}", user.Id);
        }else{
            throw;
        }
        }
        }  

        private void ExecuteSimpleQuery(string databaseName, string collectionName){
            FeedOptions queryOptions = new FeedOptions{
                MaxItemCount = -1, EnableCrossPartitionQuery = true
            };

            IQueryable<User> userQuery = this.client.CreateDocumentQuery<User>(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions).Where(u => u.LastName == "Pindakova");

            Console.WriteLine("Running LINQ query...");
            foreach(User user in userQuery){
                Console.WriteLine("\t Read {0}", user);
            }

            IQueryable<User> userQueryInSql = this.client.CreateDocumentQuery<User>(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), "SELECT * FROM User WHERE User.lastName = 'Pindavoka'", queryOptions);

            Console.WriteLine("Running Direct SQL Query...");
            foreach(User user in userQueryInSql){
                Console.WriteLine("\t Read {0}", user);
            }

            Console.WriteLine("Pressione Qualquer Tecla Para Continuar...");
            Console.ReadKey();
        }

        public async Task RunStoredProcedure(string databaseName, string collectionName, User user){
            await client.ExecuteStoredProcedureAsync<string>(UriFactory.CreateStoredProcedureUri(databaseName, collectionName, "UpdateOrderTotal"), new RequestOptions{ PartitionKey = new PartitionKey(user.UserId)});
            Console.WriteLine("Stored Procedure Complete");
        }
    }
}
