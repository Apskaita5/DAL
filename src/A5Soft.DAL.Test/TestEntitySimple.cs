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

        public Guid? Id { get; set; }

        public string StringValue { get; set; }

        public int IntValue { get; set; }
    }
}
