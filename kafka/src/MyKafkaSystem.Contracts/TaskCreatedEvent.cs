namespace MyKafkaSystem.Contracts;

public record TaskCreatedEvent(
    Guid TaskId,
    string TaskType,
    string Payload,
    DateTime CreatedAtUtc
);
