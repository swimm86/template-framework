// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectorBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Core.DependencyInjection.Base
{
    /// <summary>
    /// Абстрактный базовый класс для реализации внедрения зависимостей слоя, внутри которого он реализован.
    /// Предоставляет шаблонный метод для внедрения зависимостей в коллекцию сервисов.
    /// </summary>
    /// <remarks>
    /// Зависимости, которые предоставляют наследники этого класса внедряются автоматически.
    /// </remarks>
    public abstract class DependencyInjectorBase
    {
        /// <summary>
        /// Логгер.
        /// </summary>
        protected readonly ILogger Logger;

        /// <inheritdoc cref="DependencyInjectorBase"/>
        /// <param name="loggerFactory">Фабрика логгеров.</param>
        protected DependencyInjectorBase(
            ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(GetType());
        }

        /// <summary>
        /// Обертка для внедрения зависимостей.
        /// </summary>
        /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
        /// <returns>Коллекция сервисов <see cref="IServiceCollection"/>.</returns>
        public IServiceCollection Inject(
            IServiceCollection serviceCollection)
        {
            try
            {
                var result = Process(serviceCollection);
                Logger.LogInformation("Dependencies injected.");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Dependencies not injected.");
                throw;
            }
        }

        /// <summary>
        /// Процесс внедрения зависимостей
        /// </summary>
        /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
        /// <returns>Коллекция сервисов <see cref="IServiceCollection"/>.</returns>
        protected abstract IServiceCollection Process(
            IServiceCollection serviceCollection);
    }
}
