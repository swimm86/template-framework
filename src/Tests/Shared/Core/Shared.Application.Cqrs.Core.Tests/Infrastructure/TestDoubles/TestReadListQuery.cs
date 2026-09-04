// ----------------------------------------------------------------------------------------------
// <copyright file="TestReadListQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class TestReadListQuery(TestPageableRequest request)
    : ReadListQuery<TestPageableRequest, TestListFilter, TestPageableResponse>(request);
