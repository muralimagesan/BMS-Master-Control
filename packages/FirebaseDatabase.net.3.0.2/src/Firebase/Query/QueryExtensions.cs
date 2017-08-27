using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Firebase.Database.Query
{
    /// <summary>
    /// Query extensions providing linq like syntax for firebase server methods.
    /// </summary>
    public static class QueryExtensions
    {
        /// <summary>
        /// Adds an auth parameter to the query.
        /// </summary>
        /// <param name="node"> The child. </param>
        /// <param name="token"> The auth token. </param>
        /// <returns> The <see cref="AuthQuery"/>. </returns>
        internal static AuthQuery WithAuth(this FirebaseQuery node, string token)
        {
            return node.WithAuth(() => token);
        }

        /// <summary>
        /// References a sub child of the existing node.
        /// </summary>
        /// <param name="node"> The child. </param>
        /// <param name="path"> The path of sub child. </param>
        /// <returns> The <see cref="ChildQuery"/>. </returns>
        public static ChildQuery Child(this ChildQuery node, string path)
        {
            return node.Child(() => path);
        }

        /// <summary>
        /// Order data by given <see cref="propertyName"/>. Note that this is used mainly for following filtering queries and due to firebase implementation
        /// the data may actually not be ordered.
        /// </summary>
        /// <param name="child"> The child. </param>
        /// <param name="propertyName"> The property name. </param>
        /// <returns> The <see cref="OrderQuery"/>. </returns>
        public static OrderQuery OrderBy(this ChildQuery child, string propertyName)
        {
            return child.OrderBy(() => propertyName);
        }

        /// <summary>
        /// Instructs firebase to send data greater or equal to the <see cref="value"/>. This must be preceded by an OrderBy query.
        /// </summary>
        /// <param name="child"> Current node. </param>
        /// <param name="value"> Value to start at. </param>
        /// <returns> The <see cref="FilterQuery"/>. </returns>
        public static FilterQuery StartAt(this ParameterQuery child, string value)
        {
            return child.StartAt(() => value);
        }

        /// <summary>
        /// Instructs firebase to send data lower or equal to the <see cref="value"/>. This must be preceded by an OrderBy query.
        /// </summary>
        /// <param name="child"> Current node. </param>
        /// <param name="value"> Value to start at. </param>
        /// <returns> The <see cref="FilterQuery"/>. </returns>
        public static FilterQuery EndAt(this ParameterQuery child, string value)
        {
            return child.EndAt(() => value);
        }

        /// <summary>
        /// Instructs firebase to send data equal to the <see cref="value"/>. This must be preceded by an OrderBy query.
        /// </summary>
        /// <param name="child"> Current node. </param>
        /// <param name="value"> Value to start at. </param>
        /// <returns> The <see cref="FilterQuery"/>. </returns>
        public static FilterQuery EqualTo(this ParameterQuery child, string value)
        {
            return child.EqualTo(() => value);
        }

        /// <summary>
        /// Instructs firebase to send data greater or equal to the <see cref="value"/>. This must be preceded by an OrderBy query.
        /// </summary>
        /// <param name="child"> Current node. </param>
        /// <param name="value"> Value to start at. </param>
        /// <returns> The <see cref="FilterQuery"/>. </returns>
        public static FilterQuery StartAt(this ParameterQuery child, double value)
        {
            return child.StartAt(() => value);
        }

        /// <summary>
        /// Instructs firebase to send data lower or equal to the <see cref="value"/>. This must be preceded by an OrderBy query.
        /// </summary>
        /// <param name="child"> Current node. </param>
        /// <param name="value"> Value to start at. </param>
        /// <returns> The <see cref="FilterQuery"/>. </returns>
        public static FilterQuery EndAt(this ParameterQuery child, double value)
        {
            return child.EndAt(() => value);
        }

        /// <summary>
        /// Instructs firebase to send data equal to the <see cref="value"/>. This must be preceded by an OrderBy query.
        /// </summary>
        /// <param name="child"> Current node. </param>
        /// <param name="value"> Value to start at. </param>
        /// <returns> The <see cref="FilterQuery"/>. </returns>
        public static FilterQuery EqualTo(this ParameterQuery child, double value)
        {
            return child.EqualTo(() => value);
        }
        
        /// <summary>
        /// Instructs firebase to send data equal to the <see cref="value"/>. This must be preceded by an OrderBy query.
        /// </summary>
        /// <param name="child"> Current node. </param>
        /// <param name="value"> Value to start at. </param>
        /// <returns> The <see cref="FilterQuery"/>. </returns>
        public static FilterQuery EqualTo(this ParameterQuery child, bool value)
        {
            return child.EqualTo(() => value);
        }  

        /// <summary>
        /// Instructs firebase to send data equal to null. This must be preceded by an OrderBy query.
        /// </summary>
        /// <param name="child"> Current node. </param>
        /// <returns> The <see cref="FilterQuery"/>. </returns>
        public static FilterQuery EqualTo(this ParameterQuery child)
        {
            return child.EqualTo(() => null);
        }        

        /// <summary>
        /// Limits the result to first <see cref="count"/> items.
        /// </summary>
        /// <param name="child"> Current node. </param>
        /// <param name="count"> Number of elements. </param>
        /// <returns> The <see cref="FilterQuery"/>. </returns>
        public static FilterQuery LimitToFirst(this ParameterQuery child, int count)
        {
            return child.LimitToFirst(() => count);
        }

        /// <summary>
        /// Limits the result to last <see cref="count"/> items.
        /// </summary>
        /// <param name="child"> Current node. </param>
        /// <param name="count"> Number of elements. </param>
        /// <returns> The <see cref="FilterQuery"/>. </returns>
        public static FilterQuery LimitToLast(this ParameterQuery child, int count)
        {
            return child.LimitToLast(() => count);
        }

        public static Task PutAsync<T>(this FirebaseQuery query, T obj)
        {
            return query.PutAsync(JsonConvert.SerializeObject(obj, query.Client.Options.JsonSerializerSettings));
        }

        public static Task PatchAsync<T>(this FirebaseQuery query, T obj)
        {
            return query.PatchAsync(JsonConvert.SerializeObject(obj, query.Client.Options.JsonSerializerSettings));
        }

        public static async Task<FirebaseObject<T>> PostAsync<T>(this FirebaseQuery query, T obj, bool generateKeyOffline = true)
        {
            var result = await query.PostAsync(JsonConvert.SerializeObject(obj, query.Client.Options.JsonSerializerSettings), generateKeyOffline);

            return new FirebaseObject<T>(result.Key, obj);
        }
    }
}
