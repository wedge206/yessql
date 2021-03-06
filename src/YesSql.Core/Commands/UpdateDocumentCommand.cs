using Dapper;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Logging;

namespace YesSql.Commands
{
    public sealed class UpdateDocumentCommand : DocumentCommand
    {
        private string _tablePrefix;

        public override int ExecutionOrder { get; } = 2;

        public UpdateDocumentCommand(Document document, string tablePrefix) : base(document)
        {
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);

            var updateCmd = "update " + dialect.QuoteForTableName(_tablePrefix + documentTable) + " set " + dialect.QuoteForColumnName("Content") + " = @Content where " + dialect.QuoteForColumnName("Id") + " = @Id;";
            logger.LogSql(updateCmd);
            return connection.ExecuteScalarAsync<int>(updateCmd, Document, transaction);
        }
    }
}
