﻿using System.Diagnostics;
using System.Linq;
using HtmlTags;
using NUnit.Framework;
using Shouldly;
using StoryTeller.Results;

namespace StoryTeller.Testing.Results
{
    [TestFixture]
    public class ReporterTester
    {
        [Test]
        public void creation_of_reports()
        {
            var logging = new Reporter();

            logging.ReporterFor<DivReport>()
                .ShouldBeTheSameAs(logging.ReporterFor<DivReport>());

            logging.ReporterFor<ListReport>()
                .ShouldBeTheSameAs(logging.ReporterFor<ListReport>());
        }

        [Test]
        public void generate_reports()
        {
            var logging = new Reporter();
            logging.ReporterFor<DivReport>().Add("1").Add("2");
            logging.ReporterFor<ListReport>().Add("3").Add("4");

            var reports = logging.GenerateReports().OrderBy(x => x.title)
                .ToArray();

            reports.Select(x => x.title)
                .ShouldHaveTheSameElementsAs("Divs", "Lists");

            reports[0].html.ShouldContain("<div>1</div>");
            reports[0].html.ShouldContain("<div>2</div>");

            reports[1].html.ShouldContain("<li>3</li>");
            reports[1].html.ShouldContain("<li>4</li>");
        }

        [Test]
        public void debug_tracing()
        {
            var logging = new Reporter();
            logging.StartDebugListening();

            Debug.WriteLine("Whoa!");
            Debug.WriteLine("You don't say");
            Debug.WriteLine("Bazinga!");

            logging.Dispose();

            Debug.WriteLine("NOT HERE");

            var html = logging.ReporterFor<DebugReporter>().ToHtml().ToString();

            html.ShouldContain("Whoa!");
            html.ShouldContain("You don&#39;t say");
            html.ShouldContain("Bazinga!");

            // The following text was written after the DebugReporter was disposed and should not be present
            html.ShouldNotContain("NOT HERE");

        }
    }

    public class DivReport : IReporter
    {
        private readonly HtmlTag _body = new HtmlTag("body");

        public HtmlTag ToHtml()
        {
            return _body;
        }

        public DivReport Add(string text)
        {
            _body.Add("div").Text(text);
            return this;
        }

        public string Title
        {
            get { return "Divs"; }
        }
    }

    public class ListReport : IReporter
    {
        private readonly HtmlTag _ul = new HtmlTag("ul");

        public HtmlTag ToHtml()
        {
            return _ul;
        }

        public ListReport Add(string text)
        {
            _ul.Add("li").Text(text);
            return this;
        }

        public string Title
        {
            get { return "Lists"; }
        }
    }
}