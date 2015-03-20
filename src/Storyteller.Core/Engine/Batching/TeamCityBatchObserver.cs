using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storyteller.Core.Grammars;
using Storyteller.Core.Model;
using Storyteller.Core.Model.Persistence;

namespace Storyteller.Core.Engine.Batching
{
    public class TeamCityBatchObserver : IBatchObserver
    {
        private readonly IBatchObserver _inner;

        public TeamCityBatchObserver(IBatchObserver inner)
        {
            _inner = inner;
        }

        public void SpecRequeued(SpecExecutionRequest request)
        {
            Console.WriteLine("Requeuing {0}, attempt # {1}", request.Specification.Name, request.Plan.Attempts + 1);
        }

        public void SpecHandled(SpecExecutionRequest request, SpecResults results)
        {
            _inner.SpecHandled(request, results);

            var name = request.Specification.Name.Escape();
            var resultText = results.Counts.ToString().Escape();

            if (results.Counts.WasSuccessful())
            {
                Console.WriteLine("##teamcity[testFinished name='{0}' message='{1}']", name, resultText);
            }
            else if (request.Specification.Lifecycle == Lifecycle.Acceptance)
            {
                
                Console.WriteLine("##teamcity[testIgnored name='{0}' message='{1}']", name,
                    "Acceptance test failed: " + resultText);
            }
            else
            {
                Console.WriteLine("##teamcity[testFailed name='{0}' details='{1}']", name,
                    resultText);
            }
        }


        public Task<IEnumerable<BatchRecord>> MonitorBatch(IEnumerable<SpecNode> nodes)
        {
            return _inner.MonitorBatch(nodes);
        }
    }
}