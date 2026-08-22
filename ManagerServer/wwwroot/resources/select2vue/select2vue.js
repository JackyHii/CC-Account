Vue.component("select2", {
    props: ['value', 'multiple', 'autocompleteFilter', 'autocompleteSubtext', 'width', 'autocompleteUrl', 'autocompleteOnChange', 'noMatchesText', 'searchingText', 'placeholder'],
    data: function () {
        return {
            data: []
        }
    },
    template: "<select></select>",
    methods: {
        normalizeItem: function (item) {
            if (item == null) return null;
            var normalized = $.extend({}, item);
            if (normalized.Key == null) normalized.Key = normalized.id;
            if (normalized.UniqueName == null) normalized.UniqueName = normalized.text;
            normalized.id = normalized.Key.toString();
            normalized.text = normalized.UniqueName;
            return normalized;
        },
        toModelItem: function (item) {
            if (item == null) return null;
            if (item.element != null) {
                var storedItem = $(item.element).data('modelItem');
                if (storedItem != null) return $.extend({}, storedItem);
            }
            var modelItem = {};
            Object.keys(item).forEach(function (key) {
                if (['id', 'text', 'title', 'selected', 'disabled', '_resultId', 'element'].indexOf(key) == -1) modelItem[key] = item[key];
            });
            modelItem.Key = item.Key == null ? item.id : item.Key;
            modelItem.UniqueName = item.UniqueName == null ? item.text : item.UniqueName;
            return modelItem;
        },
        setValue: function (value) {
            var vm = this;
            var values = this.multiple == 'true' ? (value || []) : (value == null ? [] : [value]);
            values = values.map(function (item) { return vm.normalizeItem(item); });

            var $element = $(this.$el);
            values.forEach(function (item) {
                var exists = $element.find('option').filter(function () { return this.value == item.id; }).length > 0;
                if (!exists) {
                    var option = new Option(item.text, item.id, false, false);
                    $(option).data('modelItem', vm.toModelItem(item));
                    $element.append(option);
                }
            });

            var ids = values.map(function (item) { return item.id; });
            $element.val(this.multiple == 'true' ? ids : (ids[0] || null)).trigger('change.select2');
            this.data = value;
        }
    },
    mounted: function () {
        var vm = this;
        this.$el.multiple = this.multiple == 'true';
        if (!this.$el.multiple) this.$el.appendChild(new Option('', '', false, false));

        $(this.$el)
            .select2({
                allowClear: true,
                dropdownAutoWidth: true,
                placeholder: this.placeholder,
                width: this.width || '100%',
                templateResult: function (item) {
                    if (!vm.autocompleteSubtext || item[vm.autocompleteSubtext] == null) return item.UniqueName || item.text;

                    return $('<div class="flex justify-between items-center gap-3"></div>')
                        .append($('<div></div>').text(item.UniqueName || item.text))
                        .append($('<div class="opacity-50"></div>').text(item[vm.autocompleteSubtext].UniqueName));
                },
                templateSelection: function (item) { return item.UniqueName || item.text; },
                language: {
                    noResults: function () { return vm.noMatchesText; },
                    searching: function () { return vm.searchingText; }
                },
                ajax: {
                    url: function () { return vm.autocompleteUrl; },
                    dataType: 'json',
                    delay: 100,
                    data: function (params) {
                        return {
                            Term: params.term || '',
                            Page: params.page || 1,
                            Filter: vm.autocompleteFilter
                        };
                    },
                    processResults: function (data, params) {
                        params.page = params.page || 1;
                        return {
                            results: data.results.map(function (item) { return vm.normalizeItem(item); }),
                            pagination: { more: data.more }
                        };
                    }
                }
            })
            .on('change.select2-vue', function () {
                var selected = $(this).select2('data')
                    .filter(function (item) { return item.id != null && item.id.toString().length > 0; })
                    .map(function (item) { return vm.toModelItem(item); });
                vm.data = vm.multiple == 'true' ? selected : (selected[0] || null);
                vm.$emit('input', vm.data);
                vm.autocompleteOnChange(vm.data);
            });

        this.setValue(this.value);
    },
    watch: {
        value: function (value) {
            if (this.data != value) this.setValue(value);
        }
    },
    destroyed: function () {
        $(this.$el)
            .off('.select2-vue')
            .select2('destroy');
    }
});

Vue.component("input-decimal", {
    props: ['value', 'groupSeparator', 'decimalSeparator', 'minWidth', 'nullable', 'placeholder'],
    data: function () {
        return {
            number: null
        }
    },
    template: "<input type='text' class='form-control field-sizing-content' style='text-align: right; min-inline-size: 10ch' />",
    mounted: function () {
        var vm = this;
        this.number = this.value;
        if (this.nullable == 'true') {
            if (this.number != null) this.$el.value = this.number.toString().replace('.', decimalSeparator);
        }
        else {
            if (this.number != 0) this.$el.value = this.number.toString().replace('.', decimalSeparator);
            this.$el.placeholder = this.placeholder;
        }
        this.$el.style.minWidth = this.minWidth;
        $(this.$el).on('input', function () {
            var text = $(this).val() || '';
            text = text.replaceAll(vm.groupSeparator, '');
            text = text.replaceAll(vm.decimalSeparator, '.');
            text = text.replaceAll(' ', '');
            if (text.length == 0) {
                if (vm.nullable == 'true') {
                    vm.number = null;
                    vm.$emit('input', null);
                }
                else {
                    vm.number = 0;
                    vm.$emit('input', 0);
                }
            }
            else {
                try { var parsedNumber = new Mexp().eval(text); } catch (e) { }
                if (typeof parsedNumber != 'number') {
                    $(this).parent().addClass('has-error');
                }
                else {
                    if (text.includes('*') || text.includes('/') || text.includes('+') || text.includes('-')) {
                        vm.number = (parsedNumber.toPrecision(14) * 1);
                    }
                    else {
                        vm.number = parseFloat(text);
                    }
                    vm.$emit('input', vm.number);
                    $(this).parent().removeClass('has-error');
                }
            }
        });        
    },
    watch: {
        value: function (value) {
            if (this.number != value) {
                // update value
                this.number = value;
                $(this.$el).val((this.number == 0 || this.number == null) ? '' : this.number.toString().replace('.', decimalSeparator))
            }
        }
    }
});

Vue.component("liquid-editor", {
    props: ['value'],
    template: "<div />",
    mounted: function () {
        var vm = this;
        var editor = ace.edit(this.$el);
        editor.getSession().setUseWorker(false);
        editor.getSession().setMode('ace/mode/liquid');
        editor.setOption("displayIndentGuides", false);
        if (this.value) editor.setValue(this.value, -1);
        editor.on('blur', function (e) { vm.$emit('input', editor.getValue()) });
    }
});

Vue.component("html-editor", {
    props: ['value'],
    template: "<div />",
    mounted: function () {
        var vm = this;
        var editor = ace.edit(this.$el);
        editor.getSession().setUseWorker(false);
        editor.getSession().setMode('ace/mode/html');
        editor.setOption("displayIndentGuides", false);
        if (this.value) editor.setValue(this.value, -1);
        editor.on('blur', function (e) { vm.$emit('input', editor.getValue()) });
    }
});

Vue.component("javascript-editor", {
    props: ['value'],
    template: "<div />",
    mounted: function () {
        var vm = this;
        var editor = ace.edit(this.$el);
        editor.getSession().setUseWorker(false);
        editor.getSession().setMode('ace/mode/javascript');
        editor.setOption("displayIndentGuides", false);
        if (this.value) editor.setValue(this.value, -1);
        editor.on('blur', function (e) { vm.$emit('input', editor.getValue()) });
    }
});

function format(html) {
    var tab = '';
    var result = '';
    var indent = '';

    html.split(/>\s*</).forEach(function (element) {
        if (element.match(/^\/\w/)) {
            indent = indent.substring(tab.length);
        }

        result += indent + '<' + element + '>\r\n';

        if (element.match(/^<?\w[^>]*[^\/]$/) && !element.startsWith('input')) {
            indent += tab;
        }
    });

    return result.substring(1, result.length - 3);
}

if (!Number.prototype.getDecimals) {
    Number.prototype.getDecimals = function () {
        var num = parseFloat(this.toFixed(10));
        var match = ('' + num).match(/(?:\.(\d+))?(?:[eE]([+-]?\d+))?$/);
        if (!match)
            return 0;
        return Math.max(0, (match[1] ? match[1].length : 0) - (match[2] ? +match[2] : 0));
    }
}

String.prototype.replaceAll = function (target, replacement) {
    return this.split(target).join(replacement);
};
