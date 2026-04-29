using MediatR;

public record SearchCasesQuery() : IRequest<List<object>>;
