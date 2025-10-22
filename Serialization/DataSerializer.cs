using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SINEATER.Serialization
{
    public static class DataSerializer
    {
        public static T Load<T>(string json)
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T), _types);
                stream.Position = 0; // Ensure begining
                T result = (T)ser.ReadObject(stream);
                return result;
            }
        }

        // Types that can be deserialized
        private static List<Type> _types = new List<Type> {
            typeof(SkirmishStep_Appear),
            typeof(SkirmishStep_Forwards),
            typeof(SkirmishStep_Backwards),
            typeof(SkirmishStep_SidestepLeft),
            typeof(SkirmishStep_SidestepRight),
            typeof(SkirmishStep_AttackFront),
            typeof(SkirmishStep_AttackBack),
            typeof(SkirmishStep_AttackHand),
            typeof(SkirmishStep_AttackLeft),
            typeof(SkirmishStep_AttackRight),
            typeof(SkirmishStep_AttackRanged),
            typeof(Trait),
            typeof(TraitKnockback),
            typeof(TraitForceful),
            typeof(TraitSneaky),
            typeof(TraitProficient),
            typeof(TraitBalanced),
            typeof(TraitSkilled),
            typeof(TraitPadded),
            typeof(TraitHeavy),
            typeof(TraitWise),
            typeof(TraitFrenzied),
            typeof(TraitEagleEyed),
            typeof(TraitProne),
            typeof(TraitBlind),
            typeof(TraitCritical),
            typeof(TraitCrippledLeftHand),
            typeof(TraitCrippledRightHand),
            typeof(TraitParalyzed)
        };
    }
}
