type ContentCardItem = {
  id: string
  title: string
  summary: string
  categoryName: string
  displayTime: string
}

Component({
  properties: {
    item: {
      type: Object,
      value: {} as ContentCardItem,
    },
  },
  methods: {
    onOpen() {
      const item = this.properties.item as ContentCardItem
      if (!item?.id) {
        return
      }

      this.triggerEvent('open', { id: item.id })
    },
  },
})
