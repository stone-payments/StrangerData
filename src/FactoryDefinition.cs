using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrangerData
{
    public class FactoryDefinition
    {
        private readonly IDictionary<string, object> _generatedValuesDict;

        public FactoryDefinition(IDictionary<string, object> generatedValuesDict)
        {
            _generatedValuesDict = generatedValuesDict;
        }

        public void WithValue(string columnName, object value)
        {
            _generatedValuesDict[columnName] = value;
        }
    }
}
