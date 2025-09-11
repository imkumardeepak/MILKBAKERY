/**
 * jQuery Table2Excel Plugin v1.1.1
 * https://github.com/rainabba/jquery-table2excel
 *
 * Copyright 2017 rainabba, released under the MIT license
 */
;
(function($, window, document, undefined) {
    var pluginName = "table2excel",

        defaults = {
            exclude: ".noExl",
            name: "Table2Excel",
            filename: "table2excel",
            fileext: ".xls",
            exclude_img: true,
            exclude_links: true,
            exclude_inputs: true
        };

    // The actual plugin constructor
    function Plugin(element, options) {
        this.element = element;
        // jQuery has an extend method which merges the contents of two or
        // more objects, storing the result in the first object. The first object
        // is generally empty as we don't want to alter the default options for
        // future instances of the plugin
        this.settings = $.extend({}, defaults, options);
        this._defaults = defaults;
        this._name = pluginName;
        this.init();
    }

    Plugin.prototype = {
        init: function() {
            var e = this;

            var utf8Heading = "<meta http-equiv=\"content-type\" content=\"application/vnd.ms-excel; charset=UTF-8\">";
            e.template = {
                head: "<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">" + utf8Heading + "<head><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets>",
                sheet: {
                    head: "<x:ExcelWorksheet><x:Name>",
                    tail: "</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet>"
                },
                mid: "</x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--></head><body>",
                table: {
                    head: "<table>",
                    tail: "</table>"
                },
                foot: "</body></html>"
            };

            e.tableRows = [];

            // get contents of table except for exclude
            $(e.element).each(function(i, o) {
                var tempRows = "";
                $(o).find("tr").not(e.settings.exclude).each(function(i, p) {
                    tempRows += "<tr>";
                    $(p).find("td,th").not(e.settings.exclude).each(function(i, q) {
                        var flag = $(q).find(e.settings.exclude); // does this <td> have something with an exclude class
                        if (flag.length >= 1) {
                            tempRows += "<td> </td>"; // exclude it!!
                        } else {
                            tempRows += "<td>" + $(q).html() + "</td>";
                        }
                    });

                    tempRows += "</tr>";
                });
                e.tableRows.push(tempRows);
            });

            e.tableToExcel(e.tableRows, e.settings.name, e.settings.sheetName);
        },

        tableToExcel: function(table, name, sheetName) {
            var e = this,
                fullTemplate = "",
                i,
                link,
                a;

            e.format = function(s, c) {
                return s.replace(/{(\w+)}/g, function(m, p) {
                    return c[p];
                });
            };

            sheetName = typeof sheetName === "undefined" ? "Sheet" : sheetName;

            e.ctx = {
                worksheet: name || "Worksheet",
                table: table,
                sheetName: sheetName
            };

            fullTemplate = e.template.head;

            if ($.isArray(table)) {
                for (i in table) {
                    // Convert to markdown for debugging
                    fullTemplate += e.template.sheet.head + "{worksheet" + i + "}" + e.template.sheet.tail;
                }
            }

            fullTemplate += e.template.mid;

            if ($.isArray(table)) {
                for (i in table) {
                    fullTemplate += e.template.table.head + "{table" + i + "}" + e.template.table.tail;
                }
            }

            fullTemplate += e.template.foot;

            for (i in table) {
                e.ctx["worksheet" + i] = sheetName + i;
                e.ctx["table" + i] = table[i];
            }

            delete e.ctx.table;

            var isIE = /*@cc_on!@*/ false || !!document.documentMode; // this works with IE10 and IE11 both :) 
            
            if (isIE) {
                if (typeof Blob !== "undefined") {
                    // use Blob if supported
                    fullTemplate = e.format(fullTemplate, e.ctx);
                    var blob = new Blob([fullTemplate], {
                        type: "application/vnd.ms-excel"
                    });
                    window.navigator.msSaveBlob(blob, e.settings.filename + e.settings.fileext);
                }
            } else {
                var blob = new Blob([e.format(fullTemplate, e.ctx)], {
                    type: "application/vnd.ms-excel"
                });
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement("a");
                a.href = url;
                a.download = e.settings.filename + e.settings.fileext;
                a.click();
                window.URL.revokeObjectURL(url);
            }
        }
    };

    // A really lightweight plugin wrapper around the constructor,
    // preventing against multiple instantiations
    $.fn[pluginName] = function(options) {
        var e = this;
        e.each(function() {
            if (!$.data(e, "plugin_" + pluginName)) {
                $.data(e, "plugin_" + pluginName, new Plugin(this, options));
            }
        });

        // chain jQuery functions
        return e;
    };

})(jQuery, window, document);