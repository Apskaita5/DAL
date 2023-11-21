using A5Soft.DAL.Core.MicroOrm;
using System;

namespace A5Soft.DAL.Test
{
    class TestEntitySimple
    {
        private static readonly IdentityMapParentGuid<TestEntitySimple> _identityMap =
            new IdentityMapParentGuid<TestEntitySimple>("simple_test_entities", "id",
                nameof(Id), () => new TestEntitySimple(), (c) => c.Id,
                (c, v) => c.Id = v);

        private static readonly FieldMapString<TestEntitySimple> _nameMap = new FieldMapString<TestEntitySimple>(
            "string_value", nameof(StringValue), (c, v) => c.StringValue = v,
            (c) => c.StringValue, FieldPersistenceType.CRUD);
        private static readonly FieldMapInt32<TestEntitySimple> _includeForSimpleClaimMap =
            new FieldMapInt32<TestEntitySimple>(
            "int_value", nameof(IntValue),
            (c, v) => c.IntValue = v,
            (c) => c.IntValue, FieldPersistenceType.CRUD);
        private static readonly FieldMapInsertedAt<TestEntitySimple> _insertedAtMap =
            new FieldMapInsertedAt<TestEntitySimple>("inserted_at", nameof(InsertedAt),
                (c, v) => c.InsertedAt = v, (c) => c.InsertedAt);
        private static readonly FieldMapInsertedBy<TestEntitySimple> _insertedByMap =
            new FieldMapInsertedBy<TestEntitySimple>("inserted_by", nameof(InsertedBy),
                (c, v) => c.InsertedBy = v, (c) => c.InsertedBy);
        private static readonly FieldMapUpdatedAt<TestEntitySimple> _updatedAtMap =
            new FieldMapUpdatedAt<TestEntitySimple>("updated_at", nameof(UpdatedAt),
                (c, v) => c.UpdatedAt = v, (c) => c.UpdatedAt);
        private static readonly FieldMapUpdatedBy<TestEntitySimple> _updatedByMap =
            new FieldMapUpdatedBy<TestEntitySimple>("updated_by", nameof(UpdatedBy),
                (c, v) => c.UpdatedBy = v, (c) => c.UpdatedBy);

        public Guid? Id { get; set; }

        public string StringValue { get; set; }

        public int IntValue { get; set; }

        public DateTime InsertedAt { get; set; }

        public string InsertedBy { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string UpdatedBy { get; set; }
    }
}
