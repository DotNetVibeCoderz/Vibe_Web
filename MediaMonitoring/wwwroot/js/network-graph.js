// network-graph.js - Network visualization dengan D3.js Force-Directed Graph

window.initializeNetworkGraph = function(graphData) {
    console.log('Initializing D3.js network graph with data:', graphData);
    
    // Check if D3 is loaded
    if (typeof d3 === 'undefined') {
        console.error('D3.js is not loaded!');
        document.getElementById('networkGraph').innerHTML = 
            '<div style="display:flex;justify-content:center;align-items:center;height:100%;background:#ffe6e6;">' +
            '<p style="color:red;font-weight:bold;">D3.js not loaded. Please check internet connection.</p>' +
            '</div>';
        return;
    }

    var container = document.getElementById('networkGraph');
    if (!container) {
        console.error('Container #networkGraph not found!');
        return;
    }

    // Clear existing content
    container.innerHTML = '';

    var width = container.clientWidth || 800;
    var height = container.clientHeight || 400;

    // Create SVG
    var svg = d3.select('#networkGraph')
        .append('svg')
        .attr('width', width)
        .attr('height', height)
        .attr('style', 'background: #fafafa; border: 2px solid #000;');

    // Create groups for arrows, links, and nodes
    var arrowMarker = svg.append('defs')
        .append('marker')
        .attr('id', 'arrowhead')
        .attr('viewBox', '-0 -5 10 10')
        .attr('refX', 20)
        .attr('refY', 0)
        .attr('orient', 'auto')
        .attr('markerWidth', 6)
        .attr('markerHeight', 6)
        .attr('xoverflow', 'visible')
        .append('path')
        .attr('d', 'M 0,-5 L 10 ,0 L 0,5')
        .attr('fill', '#666')
        .style('stroke', 'none');

    var linkGroup = svg.append('g').attr('class', 'links');
    var nodeGroup = svg.append('g').attr('class', 'nodes');
    var labelGroup = svg.append('g').attr('class', 'labels');

    // Extract unique nodes from the graph data
    var nodesMap = new Map();
    var links = [];

    // Add source nodes (media sources)
    graphData.forEach(function(item) {
        if (!nodesMap.has(item.source)) {
            nodesMap.set(item.source, {
                id: item.source,
                type: 'source',
                group: 1,
                postCount: 0
            });
        }
        var sourceNode = nodesMap.get(item.source);
        sourceNode.postCount += parseInt(item.count);

        // Add author nodes
        if (!nodesMap.has(item.author)) {
            nodesMap.set(item.author, {
                id: item.author,
                type: 'author',
                group: 2,
                postCount: 0
            });
        }
        var authorNode = nodesMap.get(item.author);
        authorNode.postCount += parseInt(item.count);

        // Create link
        links.push({
            source: item.source,
            target: item.author,
            value: item.count
        });
    });

    var nodes = Array.from(nodesMap.values());

    console.log('Network data:', nodes.length, 'nodes,', links.length, 'links');

    if (nodes.length === 0) {
        svg.append('text')
            .attr('x', width / 2)
            .attr('y', height / 2)
            .attr('text-anchor', 'middle')
            .style('font-family', 'Arial')
            .style('font-size', '14px')
            .style('fill', '#666')
            .text('No network data available');
        return;
    }

    // Create force simulation
    var simulation = d3.forceSimulation(nodes)
        .force('link', d3.forceLink(links).id(function(d) { return d.id; }).distance(100))
        .force('charge', d3.forceManyBody().strength(-300))
        .force('center', d3.forceCenter(width / 2, height / 2))
        .force('collide', d3.forceCollide().radius(30));

    // Draw links
    var link = linkGroup.selectAll('line')
        .data(links)
        .enter()
        .append('line')
        .attr('stroke-width', function(d) { return Math.sqrt(d.value) * 2; })
        .attr('stroke', '#666')
        .attr('opacity', 0.6);

    // Draw nodes
    var node = nodeGroup.selectAll('circle')
        .data(nodes)
        .enter()
        .append('circle')
        .attr('r', function(d) { 
            var baseRadius = d.type === 'source' ? 25 : 15;
            var sizeBonus = Math.min(15, d.postCount / 5);
            return baseRadius + sizeBonus;
        })
        .attr('fill', function(d) { 
            return d.type === 'source' ? '#000' : '#666'; 
        })
        .attr('stroke', '#fff')
        .attr('stroke-width', 2)
        .call(d3.drag()
            .on('start', dragstarted)
            .on('drag', dragged)
            .on('end', dragended));

    // Add labels
    var label = labelGroup.selectAll('text')
        .data(nodes)
        .enter()
        .append('text')
        .attr('dx', function(d) { return d.type === 'source' ? 0 : 0; })
        .attr('dy', function(d) { return d.type === 'source' ? 35 : -20; })
        .attr('text-anchor', 'middle')
        .text(function(d) { 
            var text = d.id.length > 15 ? d.id.substring(0, 12) + '...' : d.id;
            return text;
        })
        .attr('font-size', '11px')
        .attr('font-family', 'Courier New, monospace')
        .attr('font-weight', 'bold')
        .attr('fill', '#000')
        .attr('pointer-events', 'none');

    // Add tooltips
    node.append('title')
        .text(function(d) {
            return d.type === 'source' 
                ? `${d.id}\nPosts: ${d.postCount}\nType: Media Source`
                : `${d.id}\nPosts: ${d.postCount}\nType: Author/Influencer`;
        });

    // Update positions on tick
    simulation.on('tick', function() {
        link
            .attr('x1', function(d) { return d.source.x; })
            .attr('y1', function(d) { return d.source.y; })
            .attr('x2', function(d) { return d.target.x; })
            .attr('y2', function(d) { return d.target.y; });

        node
            .attr('cx', function(d) { return d.x; })
            .attr('cy', function(d) { return d.y; });

        label
            .attr('x', function(d) { return d.x; })
            .attr('y', function(d) { return d.y; });
    });

    // Drag functions
    function dragstarted(event, d) {
        if (!event.active) simulation.alphaTarget(0.3).restart();
        d.fx = d.x;
        d.fy = d.y;
    }

    function dragged(event, d) {
        d.fx = event.x;
        d.fy = event.y;
    }

    function dragended(event, d) {
        if (!event.active) simulation.alphaTarget(0);
        d.fx = null;
        d.fy = null;
    }

    // Add zoom functionality
    var zoom = d3.zoom()
        .scaleExtent([0.1, 4])
        .on('zoom', function(event) {
            svg.selectAll('g').attr('transform', event.transform);
        });

    svg.call(zoom);

    console.log('Network graph initialized successfully');
};

window.updateNetworkGraph = function(newData) {
    console.log('Updating network graph...');
    // Can be extended to update existing graph with new data
};