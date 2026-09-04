// ----------------------------------------------------------------------------------------------
// <copyright file="TestPageableRequest.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Requests;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public record TestPageableRequest : PageableRequest<TestListFilter>;
