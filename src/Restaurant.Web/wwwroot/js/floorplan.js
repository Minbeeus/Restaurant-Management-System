window.floorplan = {
    tables: [],
    gridSize: 20,
    dotNetRef: null,
    activeTable: null,
    offsetX: 0,
    offsetY: 0,

    init: function (dotNetReference, tablesData) {
        this.dotNetRef = dotNetReference;
        this.tables = tablesData || [];
        
        const canvas = document.getElementById('floorplan-canvas');
        if (!canvas) return;

        // Cleanup previous listeners if any
        canvas.removeEventListener('pointerdown', this.onPointerDown);
        canvas.removeEventListener('pointermove', this.onPointerMove);
        canvas.removeEventListener('pointerup', this.onPointerUp);
        canvas.removeEventListener('pointerleave', this.onPointerLeave);

        // Bind 'this' context
        this.onPointerDown = this.onPointerDown.bind(this);
        this.onPointerMove = this.onPointerMove.bind(this);
        this.onPointerUp = this.onPointerUp.bind(this);
        this.onPointerLeave = this.onPointerLeave.bind(this);

        canvas.addEventListener('pointerdown', this.onPointerDown);
        canvas.addEventListener('pointermove', this.onPointerMove);
        canvas.addEventListener('pointerup', this.onPointerUp);
        canvas.addEventListener('pointerleave', this.onPointerLeave);
        
        // Disable native drag and drop to avoid conflicts
        canvas.ondragstart = () => false;
    },

    destroy: function() {
        const canvas = document.getElementById('floorplan-canvas');
        if (canvas) {
            canvas.removeEventListener('pointerdown', this.onPointerDown);
            canvas.removeEventListener('pointermove', this.onPointerMove);
            canvas.removeEventListener('pointerup', this.onPointerUp);
            canvas.removeEventListener('pointerleave', this.onPointerLeave);
        }
        this.tables = [];
        this.dotNetRef = null;
    },

    updateTables: function(tablesData) {
        this.tables = tablesData || [];
    },

    onPointerDown: function (e) {
        // Ensure we are in edit mode. Edit mode should probably add a class to the canvas.
        const canvas = document.getElementById('floorplan-canvas');
        if (!canvas.classList.contains('edit-mode')) return;

        const target = e.target.closest('.table-element');
        if (!target) return;

        e.preventDefault();

        const tableId = parseInt(target.getAttribute('data-id'));
        this.activeTable = this.tables.find(t => t.tableId === tableId);
        
        if (!this.activeTable) return;

        // Bắt đầu drag
        target.setPointerCapture(e.pointerId);
        target.classList.add('dragging');

        // Tính toán offset của con trỏ so với góc trên trái của bàn
        const rect = target.getBoundingClientRect();
        const canvasRect = canvas.getBoundingClientRect();
        
        // Tọa độ click tương đối với canvas
        const clickX = e.clientX - canvasRect.left;
        const clickY = e.clientY - canvasRect.top;

        this.offsetX = clickX - this.activeTable.positionX;
        this.offsetY = clickY - this.activeTable.positionY;
    },

    onPointerMove: function (e) {
        if (!this.activeTable) return;

        const canvas = document.getElementById('floorplan-canvas');
        const canvasRect = canvas.getBoundingClientRect();
        
        // Tọa độ chuột mới tương đối với canvas
        let newX = e.clientX - canvasRect.left - this.offsetX;
        let newY = e.clientY - canvasRect.top - this.offsetY;

        // Snap to grid
        newX = Math.round(newX / this.gridSize) * this.gridSize;
        newY = Math.round(newY / this.gridSize) * this.gridSize;

        // Boundary Check
        if (newX < 0) newX = 0;
        if (newY < 0) newY = 0;
        if (newX + this.activeTable.width > canvasRect.width) newX = canvasRect.width - this.activeTable.width;
        if (newY + this.activeTable.height > canvasRect.height) newY = canvasRect.height - this.activeTable.height;

        this.activeTable.positionX = newX;
        this.activeTable.positionY = newY;

        // Cập nhật DOM tạm thời
        const element = document.querySelector(`.table-element[data-id='${this.activeTable.tableId}']`);
        if (element) {
            element.style.transform = `translate(${newX}px, ${newY}px)`;
        }

        // Check va chạm (Collision)
        this.checkCollisions();
    },

    onPointerUp: function (e) {
        if (!this.activeTable) return;
        
        const target = document.querySelector(`.table-element[data-id='${this.activeTable.tableId}']`);
        if (target) {
            target.releasePointerCapture(e.pointerId);
            target.classList.remove('dragging');
        }

        const hasCollision = this.checkCollisions();

        // Gửi kết quả về Blazor
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync(
                'OnTableMoved', 
                this.activeTable.tableId, 
                this.activeTable.positionX, 
                this.activeTable.positionY,
                hasCollision
            );
        }

        this.activeTable = null;
    },

    onPointerLeave: function(e) {
        // Handle case where pointer leaves canvas entirely during drag
        if (this.activeTable) {
            this.onPointerUp(e);
        }
    },

    checkCollisions: function() {
        let hasGlobalCollision = false;

        // Clear tất cả viền đỏ trước
        document.querySelectorAll('.table-element').forEach(el => el.classList.remove('collision'));

        for (let i = 0; i < this.tables.length; i++) {
            for (let j = i + 1; j < this.tables.length; j++) {
                const t1 = this.tables[i];
                const t2 = this.tables[j];

                if (this.rectIntersect(t1, t2)) {
                    hasGlobalCollision = true;
                    const el1 = document.querySelector(`.table-element[data-id='${t1.tableId}']`);
                    const el2 = document.querySelector(`.table-element[data-id='${t2.tableId}']`);
                    if (el1) el1.classList.add('collision');
                    if (el2) el2.classList.add('collision');
                }
            }
        }
        
        return hasGlobalCollision;
    },

    rectIntersect: function(r1, r2) {
        // Handle rotation for width/height bounding box (simplified for 0,90,180,270)
        let w1 = (r1.rotation % 180 !== 0) ? r1.height : r1.width;
        let h1 = (r1.rotation % 180 !== 0) ? r1.width : r1.height;
        let w2 = (r2.rotation % 180 !== 0) ? r2.height : r2.width;
        let h2 = (r2.rotation % 180 !== 0) ? r2.width : r2.height;

        return !(r2.positionX >= r1.positionX + w1 || 
                 r2.positionX + w2 <= r1.positionX || 
                 r2.positionY >= r1.positionY + h1 || 
                 r2.positionY + h2 <= r1.positionY);
    }
};
