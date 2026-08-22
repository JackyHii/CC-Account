using ManagerServer.Api.Businesses.Business;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ManagerServer.Pdf
{
    internal sealed class SalesInvoicePdfRenderer
    {
        private static readonly Color Ink = Color.FromHex("#25282D");
        private static readonly Color Muted = Color.FromHex("#62676F");
        private static readonly Color Rule = Color.FromHex("#D9DDE2");
        private static readonly Color Danger = Color.FromHex("#8F1D1D");

        private readonly View Model;
        private readonly byte[] Logo;

        public SalesInvoicePdfRenderer(View model, byte[] logo)
        {
            Model = model;
            Logo = logo;
        }

        public byte[] Generate()
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15, Unit.Millimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Noto Sans SC").FontSize(9).FontColor(Ink).LineHeight(1.25f));

                    if (Model.Direction == Direction.Rtl) page.ContentFromRightToLeft();

                    page.Header().Element(ComposeHeader);
                    page.Content().PaddingTop(14).Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();
        }

        private void ComposeHeader(IContainer container)
        {
            container
                .BorderBottom(2)
                .BorderColor(Ink)
                .PaddingBottom(12)
                .Row(row =>
                {
                    row.RelativeItem()
                        .AlignBottom()
                        .Text(Model.Title)
                        .FontSize(26)
                        .SemiBold()
                        .FontColor(Ink);

                    if (Logo != null && Logo.Length > 0)
                    {
                        row.ConstantItem(120)
                            .Height(55)
                            .AlignRight()
                            .Image(Logo)
                            .FitArea();
                    }
                });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(14);
                column.Item().ShowEntire().Element(ComposeParties);

                if (!string.IsNullOrWhiteSpace(Model.Table.Description))
                {
                    column.Item().Text(Model.Table.Description).FontColor(Muted);
                }

                if (Model.Table.Columns.Count > 0)
                {
                    column.Item().Element(ComposeLineItems);
                }

                if (Model.Totals != null && Model.Totals.Count > 0)
                {
                    column.Item().ShowEntire().AlignRight().Width(240).Element(ComposeTotals);
                }

                var bottomFields = Model.Fields.Where(x => !x.DisplayAtTheTop).ToArray();
                if (bottomFields.Length > 0)
                {
                    column.Item().ShowEntire().Element(x => ComposeFields(x, bottomFields));
                }

                foreach (var footer in Model.Footers)
                {
                    var text = Regex.Replace(footer, @"<br\s*/?>|</p>|</div>|</li>", "\n", RegexOptions.IgnoreCase);
                    text = WebUtility.HtmlDecode(Regex.Replace(text, "<[^>]+>", string.Empty)).Trim();
                    if (!string.IsNullOrWhiteSpace(text)) column.Item().Text(text).FontColor(Muted);
                }

                if (Model.Status != null && Model.Status.Tone != Tone.Positive)
                {
                    var statusColor = Model.Status.Tone == Tone.Negative ? Danger : Ink;
                    column.Item()
                        .ShowEntire()
                        .AlignRight()
                        .Border(1)
                        .BorderColor(statusColor)
                        .PaddingHorizontal(10)
                        .PaddingVertical(5)
                        .Text(Model.Status.Text)
                        .SemiBold()
                        .FontColor(statusColor);
                }
            });
        }

        private void ComposeParties(IContainer container)
        {
            var topFields = Model.Fields.Where(x => x.DisplayAtTheTop).ToArray();

            container
                .Border(1)
                .BorderColor(Rule)
                .Padding(13)
                .Row(row =>
                {
                    row.RelativeItem(0.75f).Element(x => ComposeFields(x, topFields));

                    row.RelativeItem(1.1f)
                        .PaddingLeft(18)
                        .Element(x => ComposeParty(x, Model.Recipient?.Name, Model.Recipient?.Address, null));

                    row.RelativeItem()
                        .PaddingLeft(18)
                        .BorderLeft(1)
                        .BorderColor(Rule)
                        .Element(x => ComposeParty(x, Model.Business?.Name, Model.Business?.Address, Model.Business?.Fields));
                });
        }

        private static void ComposeFields(IContainer container, IReadOnlyCollection<View.FieldInfo> fields)
        {
            container.Column(column =>
            {
                column.Spacing(8);
                foreach (var field in fields)
                {
                    column.Item().Column(item =>
                    {
                        item.Spacing(2);
                        item.Item().Text(field.Label).FontSize(7).SemiBold().FontColor(Muted);

                        if (field.Image != null && field.Image.Url.StartsWith("data:", StringComparison.Ordinal))
                        {
                            var comma = field.Image.Url.IndexOf(',');
                            var image = Convert.FromBase64String(field.Image.Url.Substring(comma + 1));
                            item.Item().Height(36).AlignLeft().Image(image).FitArea();
                        }
                        else
                        {
                            item.Item().Text(field.Text ?? string.Empty).SemiBold();
                        }
                    });
                }
            });
        }

        private static void ComposeParty(IContainer container, string name, string address, IReadOnlyCollection<View.FieldInfo> fields)
        {
            container.Column(column =>
            {
                column.Spacing(2);
                if (!string.IsNullOrWhiteSpace(name)) column.Item().Text(name).SemiBold().FontSize(10);
                if (!string.IsNullOrWhiteSpace(address)) column.Item().Text(address).FontColor(Muted);

                foreach (var field in fields ?? [])
                {
                    var text = string.IsNullOrWhiteSpace(field.Label) ? field.Text : field.Label + ": " + field.Text;
                    if (!string.IsNullOrWhiteSpace(text)) column.Item().Text(text).FontColor(Muted);
                }
            });
        }

        private void ComposeLineItems(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var column in Model.Table.Columns)
                    {
                        if (column.Label == "#") columns.ConstantColumn(24);
                        else columns.RelativeColumn(column.Align == Align.Start ? 2.1f : 1f);
                    }
                });

                table.Header(header =>
                {
                    foreach (var column in Model.Table.Columns)
                    {
                        var cell = AlignCell(header.Cell().Background(Ink).PaddingHorizontal(7).PaddingVertical(7), column.Align);
                        cell.Text(column.Label).SemiBold().FontColor(Colors.White).FontSize(8);
                    }
                });

                foreach (var row in Model.Table.Rows)
                {
                    for (var i = 0; i < Model.Table.Columns.Count; i++)
                    {
                        var value = row.Cells[i];
                        var cell = AlignCell(table.Cell().BorderBottom(1).BorderColor(Rule).PaddingHorizontal(7).PaddingVertical(7), Model.Table.Columns[i].Align);

                        if (value.Image != null && value.Image.Url.StartsWith("data:", StringComparison.Ordinal))
                        {
                            var comma = value.Image.Url.IndexOf(',');
                            var image = Convert.FromBase64String(value.Image.Url.Substring(comma + 1));
                            cell.Column(content =>
                            {
                                content.Spacing(3);
                                content.Item().Height(32).Image(image).FitArea();
                                if (!string.IsNullOrWhiteSpace(value.Text)) content.Item().Text(value.Text);
                            });
                        }
                        else
                        {
                            cell.Text(value.Text ?? string.Empty);
                        }
                    }
                }
            });
        }

        private IContainer AlignCell(IContainer container, Align align)
        {
            return align switch
            {
                Align.Center => container.AlignCenter(),
                Align.Start => Model.Direction == Direction.Rtl ? container.AlignRight() : container.AlignLeft(),
                Align.End => Model.Direction == Direction.Rtl ? container.AlignLeft() : container.AlignRight(),
                Align.Right => container.AlignRight(),
                _ => container.AlignLeft()
            };
        }

        private void ComposeTotals(IContainer container)
        {
            container.Column(column =>
            {
                foreach (var total in Model.Totals)
                {
                    column.Item()
                        .BorderBottom(total.Emphasis ? 1.5f : 1)
                        .BorderColor(total.Emphasis ? Ink : Rule)
                        .PaddingVertical(total.Emphasis ? 7 : 5)
                        .Row(row =>
                        {
                            var label = row.RelativeItem().AlignRight().Text(total.Label);
                            var value = row.ConstantItem(95).AlignRight().Text(total.Text);
                            if (total.Emphasis)
                            {
                                label.SemiBold().FontSize(10);
                                value.SemiBold().FontSize(10);
                            }
                            else
                            {
                                label.FontColor(Muted);
                            }
                        });
                }
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container
                .BorderTop(1)
                .BorderColor(Rule)
                .PaddingTop(8)
                .Row(row =>
                {
                    if (Model.BankAccountInfo != null && !string.IsNullOrWhiteSpace(Model.BankAccountInfo.Text))
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Spacing(2);
                            column.Item().Text(Model.BankAccountInfo.Label).FontSize(7).SemiBold().FontColor(Muted);
                            column.Item().Text(Model.BankAccountInfo.Text).SemiBold();
                        });
                    }
                    else
                    {
                        row.RelativeItem();
                    }

                    row.AutoItem().AlignBottom().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8).FontColor(Muted));
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
        }
    }
}
