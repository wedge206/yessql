using Dapper;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Indexes;
using YesSql.Logging;

namespace YesSql.Commands
{
    public sealed class CreateIndexCommand : IndexCommand
    {
        private readonly IEnumerable<int> _addedDocumentIds;

        public override int ExecutionOrder { get; } = 2;

        public CreateIndexCommand(
            IIndex index,
            IEnumerable<int> addedDocumentIds,
            string tablePrefix) : base(index, tablePrefix)
        {
            _addedDocumentIds = addedDocumentIds;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var type = Index.GetType();
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);

            if (Index is MapIndex)
            {
                var sql = Inserts(type, dialect) + " " + dialect.IdentitySelectString + " " + dialect.QuoteForColumnName("Id");
                logger.LogSql(sql);
                Index.Id = await connection.ExecuteScalarAsync<int>(sql, Index, transaction);
                var command = "update " + dialect.QuoteForTableName(_tablePrefix + type.Name) + " set " + dialect.QuoteForColumnName("DocumentId") + " = @mapid where " + dialect.QuoteForColumnName("Id") + " = @Id";
                logger.LogSql(command);
                await connection.ExecuteAsync(command, new { mapid = Index.GetAddedDocuments().Single().Id, Id = Index.Id }, transaction);
            }
            else
            {
                var reduceIndex = Index as ReduceIndex;

                var sql = Inserts(type, dialect) + " " + dialect.IdentitySelectString + " " + dialect.QuoteForColumnName("Id");
                logger.LogSql(sql);
                Index.Id = await connection.ExecuteScalarAsync<int>(sql, Index, transaction);

                var bridgeTableName = type.Name + "_" + documentTable;
                var columnList = dialect.QuoteForColumnName(type.Name + "Id") +", " + dialect.QuoteForColumnName("DocumentId");
                var bridgeSql = "insert into " + dialect.QuoteForTableName(_tablePrefix + bridgeTableName) + " (" + columnList + ") values (@Id, @DocumentId);";
                logger.LogSql(bridgeSql);
                await connection.ExecuteAsync(bridgeSql, _addedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
            }
        }
    }
}
