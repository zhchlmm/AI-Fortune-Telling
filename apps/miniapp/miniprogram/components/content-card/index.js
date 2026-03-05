"use strict";
Component({
    properties: {
        item: {
            type: Object,
            value: {},
        },
    },
    methods: {
        onOpen() {
            const item = this.properties.item;
            if (!item?.id) {
                return;
            }
            this.triggerEvent('open', { id: item.id });
        },
    },
});
