window.floorplan = {
    tables: [],
    gridSize: 20,
    dotNetRef: null,
    activeTable: null,
    offsetX: 0,
    offsetY: 0,
    _bound: false,
    scale: 1,
    _resizeObserver: null,

    updateScale: function () {
        const container = document.getElementById('floorplan-container');
        const scaler = document.getElementById('floorplan-scaler');
        if (!container || !scaler) return;

        // Container visible bounds minus padding
        const padX = 16;
        const padY = 16;
        const availW = Math.max(100, container.clientWidth - padX);
        const availH = Math.max(100, container.clientHeight - padY);

        // Native architectural floorplan canvas dimension is 1240 x 720
        const scale = Math.min(availW / 1240, availH / 720);
        this.scale = scale;
        scaler.style.transform = `scale(${scale})`;
    },

    init: function (dotNetReference, tablesData) {
        this.dotNetRef = dotNetReference;
        this.tables = tablesData || [];
        
        const canvas = document.getElementById('floorplan-canvas');
        if (!canvas) return;

        // Immediately calculate scale to fit available container bounds
        this.updateScale();

        // Attach ResizeObserver to container for real-time auto-fit
        const container = document.getElementById('floorplan-container');
        if (container && window.ResizeObserver) {
            if (this._resizeObserver) {
                this._resizeObserver.disconnect();
            }
            this._resizeObserver = new ResizeObserver(() => {
                this.updateScale();
            });
            this._resizeObserver.observe(container);
        }

        if (!this._bound) {
            this.onPointerDown = this.onPointerDown.bind(this);
            this.onPointerMove = this.onPointerMove.bind(this);
            this.onPointerUp = this.onPointerUp.bind(this);
            this.onPointerLeave = this.onPointerLeave.bind(this);
            this.onWindowResize = () => this.updateScale();
            window.addEventListener('resize', this.onWindowResize);
            this._bound = true;
        }

        // Cleanup previous listeners if any
        canvas.removeEventListener('pointerdown', this.onPointerDown);
        canvas.removeEventListener('pointermove', this.onPointerMove);
        canvas.removeEventListener('pointerup', this.onPointerUp);
        canvas.removeEventListener('pointerleave', this.onPointerLeave);

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
        if (this._resizeObserver) {
            this._resizeObserver.disconnect();
            this._resizeObserver = null;
        }
        this.tables = [];
        this.dotNetRef = null;
    },

    updateTables: function(tablesData) {
        this.tables = tablesData || [];
    },

    onPointerDown: function (e) {
        const canvas = document.getElementById('floorplan-canvas');
        if (!canvas || !canvas.classList.contains('edit-mode')) return;

        const target = e.target.closest('.table-element');
        if (!target) return;

        e.preventDefault();

        const tableId = parseInt(target.getAttribute('data-id'));
        this.activeTable = this.tables.find(t => t.tableId === tableId);
        
        if (!this.activeTable) return;

        // Bắt đầu drag
        target.setPointerCapture(e.pointerId);
        target.classList.add('dragging');

        // Bounding rect của canvas và hệ số scale
        const canvasRect = canvas.getBoundingClientRect();
        const scale = this.scale || (canvasRect.width / 1240) || 1;
        
        // Tọa độ click tương đối với canvas trong hệ tọa độ chuẩn 1240x720
        const clickX = (e.clientX - canvasRect.left) / scale;
        const clickY = (e.clientY - canvasRect.top) / scale;

        this.offsetX = clickX - this.activeTable.positionX;
        this.offsetY = clickY - this.activeTable.positionY;
    },

    onPointerMove: function (e) {
        if (!this.activeTable) return;

        const canvas = document.getElementById('floorplan-canvas');
        const canvasRect = canvas.getBoundingClientRect();
        const scale = this.scale || (canvasRect.width / 1240) || 1;
        
        // Tọa độ chuột mới tương đối với canvas trong hệ tọa độ chuẩn
        let newX = (e.clientX - canvasRect.left) / scale - this.offsetX;
        let newY = (e.clientY - canvasRect.top) / scale - this.offsetY;

        // Snap to grid
        newX = Math.round(newX / this.gridSize) * this.gridSize;
        newY = Math.round(newY / this.gridSize) * this.gridSize;

        // Boundary Check (trong hệ tọa độ 1240 x 720)
        if (newX < 0) newX = 0;
        if (newY < 0) newY = 0;
        if (newX + this.activeTable.width > 1240) newX = 1240 - this.activeTable.width;
        if (newY + this.activeTable.height > 720) newY = 720 - this.activeTable.height;

        this.activeTable.positionX = newX;
        this.activeTable.positionY = newY;

        // Cập nhật DOM
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
        if (this.activeTable) {
            this.onPointerUp(e);
        }
    },

    checkCollisions: function() {
        let hasGlobalCollision = false;

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
