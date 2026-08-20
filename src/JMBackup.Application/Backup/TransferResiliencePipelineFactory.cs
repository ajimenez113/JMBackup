using Polly;
using Polly.CircuitBreaker;

namespace JMBackup.Application.Backup;

/// <summary>
/// Disyuntor por destino (RF-160): si un recurso de red está caído, deja de intentar
/// transferencias hacia él en vez de agotar 3 reintentos con cada uno de 40 000
/// archivos. Los 3 reintentos con espera creciente (5 s, 30 s, 120 s — RF-162) los hace
/// <see cref="BackupEngine"/> como pasadas completas sobre los elementos que fallaron,
/// no este pipeline: por eso aquí solo vive el disyuntor.
/// </summary>
public static class TransferResiliencePipelineFactory
{
    public static readonly IReadOnlyList<TimeSpan> RetryPassDelays =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(120)];

    public const int MaxRetryPasses = 3;

    public static ResiliencePipeline CreateCircuitBreaker(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider,
        }
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30),
            ShouldHandle = new PredicateBuilder().Handle<Exception>(exception => exception is not OperationCanceledException),
        })
        .Build();
    }
}
