﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using FaunaDB.Collections;
using FaunaDB.Errors;
using FaunaDB.Query;
using FaunaDB.Types;
using Newtonsoft.Json;

namespace FaunaDB.Client
{
    /// <summary>
    /// C# native client for FaunaDB.
    /// <para>Queries are constructed by using static helpers in <see cref="Language"/> package.</para>
    /// </summary>
    /// <example>
    /// <code language="cs">
    /// var client = new FaunaClient(secret: "someAuthToken");
    /// client.Query(Get(Ref("some/ref")));
    /// </code>
    /// </example>
    public class FaunaClient
    {
        readonly IClientIO clientIO;

        /// <summary>
        /// Constructs a new instance of <see cref="FaunaClient"/> with the specified arguments.
        /// </summary>
        /// <param name="secret">Auth token for the FaunaDB server.</param>
        /// <param name="endpoint">URL for the FaunaDB server. Defaults to "https://db.fauna.com:443"</param>
        /// <param name="timeout">Timeout for I/O operations. Defaults to 1 minute.</param>
        public FaunaClient(
            string secret,
            string endpoint = "https://db.fauna.com:443",
            TimeSpan? timeout = null,
            HttpClient httpClient = null)
            : this(CreateClient(secret, endpoint, timeout, httpClient))
        { }

        /// <summary>
        /// Constructs a new instance of <see cref="FaunaClient"/> with the specified <see cref="IClientIO"/>.
        /// </summary>
        public FaunaClient(IClientIO clientIO)
        {
            this.clientIO = clientIO;
        }

        /// <summary>
        /// Creates a new <see cref="FaunaClient"/> with the user secret provided. Queries submitted to a session client will be
        /// authenticated with the secret provided. A session client shares its parent's internal resources.
        /// </summary>
        /// <param name="secret">Auth token for the FaunaDB server.</param>
        public FaunaClient NewSessionClient(string secret) =>
            new FaunaClient(clientIO.NewSessionClient(secret));

        /// <summary>
        /// Issues a Query to FaunaDB.
        /// <para>
        /// Queries are modeled through the FaunaDB query language, represented by the helper functions in the <see cref="Language"/> class.
        /// </para>
        /// <para>
        /// Responses are modeled as a general response tree. Each node is a <see cref="Value"/>, and
        /// can be coerced to structured types through various methods on that class.
        /// </para>
        /// </summary>
        /// <param name="expression">Expression generated by methods of <see cref="Language"/>.</param>
        public async Task<Value> Query(Expr expression) =>
            await Execute(HttpMethodKind.Post, "", expression).ConfigureAwait(false);

        /// <summary>
        /// Issues multiple queries to FaunaDB.
        /// <para>
        /// These queries are sent to FaunaDB in a single request, and are evaluated. The list of response nodes is returned
        /// in the same order as the issued queries.
        /// </para>
        /// <para>
        /// See <see cref="Query(Expr)"/> for more information on the individual queries.
        /// </para>
        /// </summary>
        /// <param name="expressions">the list of query expressions to be sent to FaunaDB.</param>
        /// <returns>a <see cref="Task"/> containing an ordered list of root response nodes.</returns>
        public async Task<Value[]> Query(params Expr[] expressions)
        {
            var response = await Query(new UnescapedArray(expressions)).ConfigureAwait(false);
            return response.Collect(Field.Root).ToArray();
        }

        /// <summary>
        /// Issues multiple queries to FaunaDB.
        /// <para>
        /// These queries are sent to FaunaDB in a single request, and are evaluated. The list of response nodes is returned
        /// in the same order as the issued queries.
        /// </para>
        /// <para>
        /// See <see cref="Query(Expr)"/> for more information on the individual queries.
        /// </para>
        /// </summary>
        /// <param name="expressions">the list of query expressions to be sent to FaunaDB.</param>
        /// <returns>a <see cref="Task"/> containing an ordered list of root response nodes.</returns>
        public async Task<IEnumerable<Value>> Query(IEnumerable<Expr> expressions)
        {
            var response = await Query(new UnescapedArray(expressions.ToList())).ConfigureAwait(false);
            return response.Collect(Field.Root);
        }

        /// <summary>
        /// Check service health.
        /// </summary>
        /// <param name="scope">Must be "node", "local", "global", or "all". Defaults to "global"</param>
        /// <param name="timeout">Time to wait for the ping to succeed, in milliseconds.</param>
        /// <returns>a <see cref="Task"/> with the message representing the result operation</returns>
        public async Task<string> Ping(string scope = null, int? timeout = null) =>
            (string)await Execute(HttpMethodKind.Get, "ping", query: ImmutableDictionary.Of("scope", scope, "timeout", timeout?.ToString())).ConfigureAwait(false);

        async Task<Value> Execute(HttpMethodKind action, string path, Expr data = null, IReadOnlyDictionary<string, string> query = null)
        {
            var dataString = data == null ?  null : JsonConvert.SerializeObject(data, Formatting.None);
            var responseHttp = await clientIO.DoRequest(action, path, dataString, query).ConfigureAwait(false);

            RaiseForStatusCode(responseHttp);

            var responseContent = FromJson(responseHttp.ResponseContent);
            return responseContent["resource"];
        }

        internal struct ErrorsWrapper
        {
            public IReadOnlyList<QueryError> Errors;
        }

        internal static void RaiseForStatusCode(RequestResult resultRequest)
        {
            var statusCode = resultRequest.StatusCode;

            if (statusCode >= 200 && statusCode < 300)
                return;

            var wrapper = JsonConvert.DeserializeObject<ErrorsWrapper>(resultRequest.ResponseContent);

            var response = new QueryErrorResponse(statusCode, wrapper.Errors);

            switch (statusCode)
            {
                case 400:
                    throw new BadRequest(response);
                case 401:
                    throw new Unauthorized(response);
                case 403:
                    throw new PermissionDenied(response);
                case 404:
                    throw new NotFound(response);
                case 500:
                    throw new InternalError(response);
                case 503:
                    throw new UnavailableError(response);
                default:
                    throw new UnknowException(response);
            }
        }

        static ObjectV FromJson(string json)
        {
            try
            {
                var settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };
                return JsonConvert.DeserializeObject<ObjectV>(json, settings);
            }
            catch (JsonReaderException ex)
            {
                throw new UnknowException($"Bad JSON: {ex}");
            }
        }

        static IClientIO CreateClient(
            string secret,
            string endpoint,
            TimeSpan? timeout,
            System.Net.Http.HttpClient httpClient = null)
        {
            return new DefaultClientIO(
                secret: secret,
                endpoint: new Uri(endpoint),
                timeout: timeout ?? TimeSpan.FromSeconds(60),
                httpClient: httpClient
            );
        }
    }
}
