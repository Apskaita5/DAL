using System;

namespace A5Soft.DAL.Core.DbSchema
{
    public sealed class CloneProgressArgs
    {

        public CloneProgressArgs(Stage currentStage, string currentTable, int rowProgress)
        {
            CurrentStage = currentStage;
            CurrentTable = currentTable ?? throw new ArgumentNullException(nameof(currentTable));
            RowProgress = rowProgress;
        }


        public enum Stage
        {
            FetchingSchema,
            CreatingSchema,
            FetchingRowCount,
            CopyingData,
            Canceled,
            Completed
        }


        public Stage CurrentStage { get; private set; }

        public string CurrentTable { get; private set; }

        public int RowProgress { get; private set; }

    }
}
