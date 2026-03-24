

window.familyTreeDownload = (fileName, content) => {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
};

window.familyTreeExport = {
    downloadText: (fileName, content, mimeType) => {
        const blob = new Blob([content], { type: mimeType || 'text/plain' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
    },
    printHtml: (title, html) => {
        const win = window.open('', '_blank');
        if (!win) {
            return;
        }
        win.document.write(html);
        win.document.close();
        win.document.title = title;
        win.focus();
        win.print();
    }
};



window.familyTreeViewer = {
    svg: null,
    g: null,
    zoom: null,
    dotNetRef: null,
    config: {
        nodeWidth: 280, // Wider for couples
        nodeHeight: 110,
        nodeGap: 60,
        levelGap: 160
    },

    init: function (svgId, minimapId, data, dotNetRef) {
        this.dotNetRef = dotNetRef;
        const svgElement = document.getElementById(svgId);
        if (!svgElement) return;

        // Clear existing
        const svg = d3.select("#" + svgId);
        svg.selectAll("*").remove();

        const width = svgElement.clientWidth || 800;
        const height = svgElement.clientHeight || 600;

        this.svg = svg;
        // Container for everything that zooms
        this.container = svg.append("g").attr("class", "zoom-container");
        // Main group for the tree
        this.g = this.container.append("g").attr("class", "main-group");

        this.zoom = d3.zoom()
            .scaleExtent([0.1, 3])
            .on("zoom", (event) => {
                this.container.attr("transform", event.transform);
                this.updateMinimap(event.transform, width, height, minimapId);
            });

        svg.call(this.zoom);

        // Build hierarchy
        // Since data is an array of roots, we wrap it
        const root = d3.hierarchy({ children: data });
        
        const treeLayout = d3.tree()
            .nodeSize([this.config.nodeWidth + this.config.nodeGap, this.config.levelGap]);
        
        treeLayout(root);

        // Draw Links
        this.g.selectAll(".link")
            .data(root.links().filter(d => d.source.depth > 0)) // skip the virtual root
            .enter()
            .append("path")
            .attr("class", "link")
            .attr("d", d => {
                // Custom path: from bottom center of parent to top center of child
                const startX = d.source.x;
                const startY = d.source.y + this.config.nodeHeight / 2;
                const endX = d.target.x;
                const endY = d.target.y - this.config.nodeHeight / 2;
                return `M${startX},${startY} C${startX},${(startY + endY) / 2} ${endX},${(startY + endY) / 2} ${endX},${endY}`;
            })
            .attr("fill", "none")
            .attr("stroke", "#000")
            .attr("stroke-width", 4);

        // Draw Nodes (Family Units)
        const nodes = this.g.selectAll(".node")
            .data(root.descendants().filter(d => d.depth > 0))
            .enter()
            .append("g")
            .attr("class", "node")
            .attr("transform", d => `translate(${d.x},${d.y})`);

        // Node card (Border/Background)
        nodes.append("rect")
            .attr("x", -this.config.nodeWidth / 2)
            .attr("y", -this.config.nodeHeight / 2)
            .attr("width", this.config.nodeWidth)
            .attr("height", this.config.nodeHeight)
            .attr("fill", "#fff")
            .attr("stroke", "#000")
            .attr("stroke-width", 4)
            .style("filter", "drop-shadow(6px 6px 0px #000)");

        // Render Person (Left half of node)
        const personGroup = nodes.append("g")
            .attr("class", "person-rect")
            .style("cursor", "pointer")
            .on("click", (event, d) => {
                this.dotNetRef.invokeMethodAsync("SelectPerson", d.data.id);
            });

        this.renderPersonBox(personGroup, -this.config.nodeWidth / 2 + 5, -this.config.nodeHeight / 2 + 5, true);

        // Render Spouses (Right half of node)
        nodes.each((d, i, nodesList) => {
            const g = d3.select(nodesList[i]);
            if (d.data.spouses && d.data.spouses.length > 0) {
                // Add separator line
                g.append("line")
                    .attr("x1", 0)
                    .attr("y1", -this.config.nodeHeight / 2 + 10)
                    .attr("x2", 0)
                    .attr("y2", this.config.nodeHeight / 2 - 10)
                    .attr("stroke", "#000")
                    .attr("stroke-width", 2)
                    .attr("stroke-dasharray", "4,4");

                const spouseGroup = g.append("g")
                    .attr("class", "spouse-rect")
                    .style("cursor", "pointer")
                    .on("click", (event) => {
                        this.dotNetRef.invokeMethodAsync("SelectPerson", d.data.spouses[0].id);
                    });

                this.renderPersonBox(spouseGroup, 5, -this.config.nodeHeight / 2 + 5, false);
            }
        });

        this.resetZoom();
        this.updateMinimap(d3.zoomIdentity, width, height, minimapId);
    },

    renderPersonBox: function(group, x, y, isMain) {
        const boxWidth = this.config.nodeWidth / 2 - 10;
        const boxHeight = this.config.nodeHeight - 10;

        // Photo placeholder
        group.append("rect")
            .attr("x", x + 5)
            .attr("y", y + 5)
            .attr("width", 50)
            .attr("height", 50)
            .attr("fill", d => isMain ? (d.data.gender === "Male" ? "#8ecae6" : "#ffb703") : (d.data.spouses && d.data.spouses[0]?.gender === "Male" ? "#8ecae6" : "#ffb703"))
            .attr("stroke", "#000")
            .attr("stroke-width", 2);

        // Photo Image
        group.append("image")
            .attr("xlink:href", d => isMain ? d.data.photo : (d.data.spouses && d.data.spouses[0]?.photo))
            .attr("x", x + 5)
            .attr("y", y + 5)
            .attr("width", 50)
            .attr("height", 50)
            .attr("preserveAspectRatio", "xMidYMid slice");

        // Name
        group.append("text")
            .attr("x", x + 5)
            .attr("y", y + 70)
            .attr("font-weight", "bold")
            .attr("font-size", "12px")
            .attr("fill", "#000")
            .text(d => {
                const name = isMain ? d.data.name : (d.data.spouses && d.data.spouses[0]?.name);
                return name ? (name.length > 15 ? name.substring(0, 13) + ".." : name) : "";
            });
        
        // Role Badge
        group.append("rect")
            .attr("x", x + 5)
            .attr("y", y + 78)
            .attr("width", 40)
            .attr("height", 14)
            .attr("fill", isMain ? "#000" : "#666");

        group.append("text")
            .attr("x", x + 25)
            .attr("y", y + 88)
            .attr("text-anchor", "middle")
            .attr("font-size", "9px")
            .attr("fill", "#fff")
            .attr("font-weight", "bold")
            .text(isMain ? "PERSON" : "SPOUSE");
    },

    updateMinimap: function (transform, width, height, minimapId) {
        const minimapSvg = d3.select("#" + minimapId);
        if (!minimapSvg.node()) return;

        minimapSvg.selectAll("*").remove();

        const mw = minimapSvg.node().clientWidth || 240;
        const mh = minimapSvg.node().clientHeight || 160;
        
        // Scale the entire tree to fit minimap
        const scale = 0.1;
        const miniG = minimapSvg.append("g")
            .attr("transform", `translate(${mw/2}, 20) scale(${scale})`);

        if (this.g) {
            const clone = this.g.node().cloneNode(true);
            miniG.node().appendChild(clone);
        }

        // Viewport rectangle in minimap
        const viewRect = minimapSvg.append("rect")
            .attr("fill", "rgba(255, 183, 3, 0.2)")
            .attr("stroke", "#ffb703")
            .attr("stroke-width", 2);

        // Map the current view to the minimap
        const vx = (-transform.x / transform.k) * scale + (mw / 2);
        const vy = (-transform.y / transform.k) * scale + 20;
        const vw = (width / transform.k) * scale;
        const vh = (height / transform.k) * scale;

        viewRect.attr("x", vx).attr("y", vy).attr("width", vw).attr("height", vh);
    },

    zoomIn: function () {
        this.svg.transition().duration(300).call(this.zoom.scaleBy, 1.3);
    },

    zoomOut: function () {
        this.svg.transition().duration(300).call(this.zoom.scaleBy, 0.7);
    },

    resetZoom: function () {
        const svgElement = this.svg.node();
        const width = svgElement.clientWidth;
        const height = svgElement.clientHeight;
        this.svg.transition().duration(500).call(
            this.zoom.transform, 
            d3.zoomIdentity.translate(width / 2, 80).scale(0.7)
        );
    }
};

// Theme helper untuk Blazor interop
window.familyTreeTheme = {
    storageKey: "familyTree.theme",

    getTheme: function () {
        const saved = localStorage.getItem(this.storageKey);
        return saved || "light";
    },

    toggleTheme: function () {
        const current = this.getTheme();
        const next = current === "dark" ? "light" : "dark";
        localStorage.setItem(this.storageKey, next);
        return next;
    }
};
