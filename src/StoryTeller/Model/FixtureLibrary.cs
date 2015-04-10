﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FubuCore;
using FubuCore.Reflection;
using FubuCore.Util;

namespace StoryTeller.Model
{
    public class FixtureLibrary
    {
        public static readonly Cache<Type, Fixture> FixtureCache =
            new Cache<Type, Fixture>(type => (Fixture) Activator.CreateInstance(type));

        public readonly Cache<string, Fixture> Fixtures = new Cache<string, Fixture>(key => new MissingFixture(key));
        public readonly Cache<string, FixtureModel> Models = new Cache<string, FixtureModel>();

        public static bool IsFixtureType(Type type)
        {
            if (!type.CanBeCastTo<Fixture>()) return false;
            if (type.HasAttribute<HiddenAttribute>()) return false;
            if (!type.IsConcreteWithDefaultCtor()) return false;
            if (type.IsOpenGeneric()) return false;

            return true;
        }

        public static IEnumerable<Type> FixtureTypesFor(Assembly assembly)
        {
            try
            {
                return assembly.GetExportedTypes().Where(IsFixtureType);
            }
            catch (Exception)
            {
                return new Type[0];
            }
        }


        public static Task<FixtureLibrary> CreateForAppDomain(CellHandling cellHandling)
        {
            var storytellerAssembly = Assembly.GetExecutingAssembly().GetName().Name;

            IEnumerable<Task<CompiledFixture>> fixtures = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.GetReferencedAssemblies().Any(assem => assem.Name == storytellerAssembly))
                .SelectMany(FixtureTypesFor)
                .Select(
                    type => { return Task.Factory.StartNew(() => CreateCompiledFixture(cellHandling, type)); });

            return Task.WhenAll(fixtures).ContinueWith(results =>
            {
                var library = new FixtureLibrary();

                results.Result.Each(x =>
                {
                    library.Fixtures[x.Fixture.Key] = x.Fixture;
                    library.Models[x.Fixture.Key] = x.Model;
                });

                return library;
            });
        }

        public static CompiledFixture CreateCompiledFixture(CellHandling cellHandling, Type type)
        {
            try
            {
                var fixture = Activator.CreateInstance(type) as Fixture;
                FixtureCache[type] = fixture;
                return new CompiledFixture
                {
                    Fixture = fixture,
                    Model = fixture.Compile(cellHandling)
                };
            }
            catch (Exception e)
            {
                var fixture = new InvalidFixture(type, e);
                var model = fixture.Compile(cellHandling);
                model.implementation = type.FullName;

                return new CompiledFixture
                {
                    Fixture = fixture,
                    Model = model
                };
            }
        }

        public struct CompiledFixture
        {
            public Fixture Fixture;
            public FixtureModel Model;
        }
    }
}