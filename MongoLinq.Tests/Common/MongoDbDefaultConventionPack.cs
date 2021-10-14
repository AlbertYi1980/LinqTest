using System.Collections.Generic;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoLinq.Tests.Common
{
    public class MongoDbDefaultConventionPack : IConventionPack
    {
        // private static fields
        private static readonly IConventionPack __defaultConventionPack = new MongoDbDefaultConventionPack();

        // private fields
        private readonly IEnumerable<IConvention> _conventions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbDefaultConventionPack" /> class.
        /// </summary>
        private MongoDbDefaultConventionPack()
        {
            _conventions = new List<IConvention>
            {
                new ReadWriteMemberFinderConvention(),
                // new NamedIdMemberConvention(new [] { "Id", "id", "_id" }), changed to:
                new NamedIdMemberConvention(),
                new NamedExtraElementsMemberConvention(new [] { "ExtraElements" }),
                // new IgnoreExtraElementsConvention(false), changed to:
                new IgnoreExtraElementsConvention(true),
                new ImmutableTypeClassMapConvention(),
                new NamedParameterCreatorMapConvention(),
                new StringObjectIdIdGeneratorConvention(), // should be before LookupIdGeneratorConvention
                new LookupIdGeneratorConvention()
            };
        }

        // public static properties
        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static IConventionPack Instance
        {
            get { return __defaultConventionPack; }
        }

        // public properties
        /// <summary>
        /// Gets the conventions.
        /// </summary>
        public IEnumerable<IConvention> Conventions
        {
            get { return _conventions; }
        }
    }
}