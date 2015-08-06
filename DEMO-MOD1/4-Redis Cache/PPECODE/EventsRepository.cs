namespace Events
{
    using Events.Models;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Claims;
    using System.Web;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
    using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling.Data;
    using Microsoft.Azure.ActiveDirectory.GraphClient;

    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure;

    using StackExchange.Redis;
    using Events.RedisExtensions;

    public class EventsRepository : IEventsRepository
    {
        private const string EventsQuery = "SELECT {0} Id, Title, [Description], Location, StartDate, [Days], AudienceId, OwnerId FROM [Event]";

        private const string RegistrationQuery = @"SELECT Id, Title, [Description], Location, StartDate, [Days], AudienceId, OwnerId
                                                   FROM [Event]
                                                   INNER JOIN [Registration] ON [Event].Id = [Registration].EventId
                                                   WHERE [Registration].UserId = @UserId";

        private const string EventsInsert = @"INSERT INTO Event (Title, Description, Location, StartDate, Days, AudienceId, OwnerId) VALUES (@Title, @Description, @Location, @StartDate, @Days, @AudienceId, @OwnerId);
                                              SELECT @@IDENTITY";

        private const string RegistrationInsert = @"INSERT INTO Registration (UserId, EventId, RegistrationDate) VALUES (@UserId, @EventId, @RegistrationDate)";

        private static IList<ActiveDirectoryUser> activeDirectoryUsers;

        public static void GetUsersFromAD()
        {
            //if (HttpContext.Current.Cache["ADUSERS"] == null)
            IDatabase cache = MvcApplication.RedisCache.GetDatabase();
            var adUsers = cache.Get<List<ActiveDirectoryUser>>("ADUSERS");
            if (adUsers == null)
            {
                // Graph API Settings
                string graphResourceId = "https://graph.windows.net";
                string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
                string appPassword = ConfigurationManager.AppSettings["ida:Password"];
                Uri audienceUri = new Uri(ConfigurationManager.AppSettings["ida:AudienceUri"]);
                string tenant = audienceUri.Host;
                string Authority = String.Format("https://login.windows.net/{0}", tenant);

                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                AuthenticationContext authContext = new AuthenticationContext(Authority);
                activeDirectoryUsers = new List<ActiveDirectoryUser>();
                ClientCredential credential = new ClientCredential(clientId, appPassword);
                AuthenticationResult result = null;
                List<User> userList = new List<User>();
                result = authContext.AcquireToken(graphResourceId, credential);

                //Setup Graph API connection and get a list of users 
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = "2013-11-08";

                GraphConnection graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);
                // Get results from all pages into a list 
                PagedResults<User> pagedResults = graphConnection.List<User>(null, new FilterGenerator());
                userList.AddRange(pagedResults.Results);
                while (!pagedResults.IsLastPage)
                {
                    pagedResults = graphConnection.List<User>(pagedResults.PageToken, new FilterGenerator());
                    userList.AddRange(pagedResults.Results);
                }
                foreach (var u in userList)
                {
                    var adUser = new ActiveDirectoryUser();
                    adUser.Location = String.Format("{0},{1}", u.City, u.State);
                    adUser.FullName = u.GivenName + " " + u.Surname;
                    adUser.Position = u.JobTitle;
                    adUser.ActiveDirectoryId = u.UserPrincipalName;
                    adUser.ObjectId = u.ObjectId;

                    try
                    {
                        using (Stream ms = graphConnection.GetStreamProperty(u, GraphProperty.ThumbnailPhoto, "image/jpeg"))
                        {
                            if (ms != null)
                            {
                                byte[] b;
                                using (BinaryReader br = new BinaryReader(ms))
                                {
                                    b = br.ReadBytes((int)ms.Length);
                                }
                                adUser.ThumbnailPhoto = b;
                                // Retrieve via the controller
                                adUser.ImageUrl = String.Format("/AdImages/Details/{0}", adUser.ObjectId);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        adUser.ImageUrl = "/images/user-placeholder.png";
                    }
                    activeDirectoryUsers.Add(adUser);
                }
                // HttpContext.Current.Cache.Insert("ADUSERS", activeDirectoryUsers, null, DateTime.Now.AddMinutes(5), TimeSpan.Zero, System.Web.Caching.CacheItemPriority.Normal, null);
                cache.Set("ADUSERS", activeDirectoryUsers, TimeSpan.FromMinutes(5));

            }
            else
            {
                //activeDirectoryUsers = (List<ActiveDirectoryUser>)HttpContext.Current.Cache["ADUSERS"];
                activeDirectoryUsers = (List<ActiveDirectoryUser>)cache.Get("ADUSERS");
            }
        }

        static EventsRepository()
        {
            GetUsersFromAD();
        }

        public EventsRepository(string userName)
        {
        }

        public Event GetEvent(int eventId)
        {
            using (var cmd = this.CreateCommand(
                string.Format(EventsQuery, string.Empty) + "WHERE Id = @EventId",
                new Dictionary<string, object>() { { "@EventId", eventId } }))
            {
                return this.EventsFromDBQuery(cmd.ExecuteReader()).FirstOrDefault();
            }
        }

        public IEnumerable<Event> UpcomingEvents(int count)
        {
            using (var cmd = this.CreateCommand(
                string.Format(EventsQuery, count > 0 ? "TOP " + count : string.Empty) + "WHERE StartDate > GETDATE() ORDER BY StartDate",
                null))
            {
                return this.EventsFromDBQuery(cmd.ExecuteReader());
            }
        }

        public IEnumerable<Event> GetUserEvents(string activeDirectoryId)
        {
            using (var cmd = this.CreateCommand(
                RegistrationQuery,
                new Dictionary<string, object>() { { "@UserId", activeDirectoryId } }))
            {
                var reader = cmd.ExecuteReader();
                return this.EventsFromDBQuery(reader);
            }
        }

        public IEnumerable<ActiveDirectoryUser> ActiveDirectoryUsers()
        {
            return activeDirectoryUsers;
        }

        public ActiveDirectoryUser ActiveDirectoryUser(string activeDirectoryId)
        {
            return activeDirectoryUsers.FirstOrDefault(u => u.ActiveDirectoryId.Equals(activeDirectoryId, StringComparison.InvariantCultureIgnoreCase));

        }

        public Event CreateEvent(Event @event)
        {
            using (var cmd = this.CreateCommand(
                EventsInsert,
                new Dictionary<string, object>() {
                { "@Title", @event.Title },
                { "@Description", @event.Description },
                { "@Location", @event.Location },
                { "@StartDate", @event.StartDate },
                { "@Days", @event.Days },
                { "@AudienceId", (int)@event.Audience },
                { "@OwnerId", @event.OwnerId }
                }))
            {
                var id = cmd.ExecuteScalar();
                @event.Id = Convert.ToInt32(id);
                return @event;
            }
        }

        public bool RegisterUser(string activeDirectoryId, int eventId)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);
            var theEvent = this.GetEvent(eventId);
            var notificationMessage = String.Format("{0};User {0} registered with event {1} on {2}. ",
                                            activeDirectoryId, theEvent.Title, DateTime.Now);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("events");
            queue.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage(notificationMessage);
            queue.AddMessage(message);

            using (var cmd = this.CreateCommand(
                RegistrationInsert,
                new Dictionary<string, object>() {
                { "@UserId", activeDirectoryId },
                { "@EventId", eventId },
                { "@RegistrationDate", DateTime.Now }
                }))
            {
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private ReliableSqlConnection GetConnection()
        {
            String conString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            ReliableSqlConnection sqlConnection = new ReliableSqlConnection(conString);
            return sqlConnection;
        }


        private IDbCommand CreateCommand(string sqlScript, IDictionary<string, object> @params)
        {
            ReliableSqlConnection connection = GetConnection();
            var command = SqlCommandFactory.CreateCommand(connection);
            command.CommandText = sqlScript;
            command.CommandType = CommandType.Text;

            if (@params != null)
            {
                foreach (var param in @params)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    dbParam.Value = param.Value;
                    command.Parameters.Add(dbParam);
                }
            }

            command.Connection.Open();

            return command;
        }

        private IEnumerable<Event> EventsFromDBQuery(IDataReader reader)
        {
            var events = new List<Event>();

            while (reader.Read())
            {
                events.Add(new Event()
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    Location = reader.GetString(3),
                    StartDate = reader.GetDateTime(4),
                    Days = reader.GetInt32(5),
                    Audience = (AudienceType)reader.GetByte(6),
                    OwnerId = reader.GetString(7)
                });
            }

            return events;
        }
    }
}