// ----------------------------------------------------------------------------------------------
// <copyright file="TestModels.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed record TestRequest(string Name = "test") : IRequest<TestResponse>;

public sealed record TestResponse(Guid Id = default);

public sealed record GetTestEntityRequest : IRequest<Shared.Testing.Entities.TestEntity>;

public sealed record GetTestEntityWithoutDeletedRequest : IRequest<Shared.Testing.Entities.TestEntityWithoutDeleted>;
