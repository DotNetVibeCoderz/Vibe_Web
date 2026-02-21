// charts.js - Visualisasi Data menggunakan D3.js untuk Media Monitoring

window.renderCategoryChart = function(categories) {
    // Membersihkan chart sebelumnya
    d3.select("#categoryChart").html("");
    
    if (!categories || categories.length === 0) {
        d3.select("#categoryChart")
            .append("text")
            .attr("x", "50%")
            .attr("y", "50%")
            .attr("text-anchor", "middle")
            .style("font-size", "14px")
            .text("NO DATA AVAILABLE");
        return;
    }

    const container = d3.select("#categoryChart");
    const width = container.node().clientWidth;
    const height = container.node().clientHeight;
    const margin = { top: 20, right: 30, bottom: 60, left: 40 };
    const chartWidth = width - margin.left - margin.right;
    const chartHeight = height - margin.top - margin.bottom;

    const svg = container.append("svg")
        .attr("width", width)
        .attr("height", height)
        .append("g")
        .attr("transform", `translate(${margin.left},${margin.top})`);

    // Scales
    const x = d3.scaleBand()
        .range([0, chartWidth])
        .domain(categories.map(d => d.category))
        .padding(0.2);

    const y = d3.scaleLinear()
        .range([chartHeight, 0])
        .domain([0, d3.max(categories, d => d.count) * 1.2]);

    // Axes
    svg.append("g")
        .attr("transform", `translate(0,${chartHeight})`)
        .call(d3.axisBottom(x))
        .selectAll("text")
        .attr("transform", "rotate(-45)")
        .style("text-anchor", "end")
        .style("font-family", "Arial")
        .style("font-size", "12px")
        .style("font-weight", "bold");

    svg.append("g")
        .call(d3.axisLeft(y).ticks(5))
        .style("font-family", "Arial")
        .style("font-size", "11px");

    // Bars dengan gaya Brutalist
    svg.selectAll("mybar")
        .data(categories)
        .enter()
        .append("rect")
        .attr("x", d => x(d.category))
        .attr("y", d => y(d.count))
        .attr("width", x.bandwidth())
        .attr("height", d => chartHeight - y(d.count))
        .attr("fill", "#000000")
        .attr("stroke", "#1a1a1a")
        .attr("stroke-width", "2px")
        .on("mouseover", function(event, d) {
            d3.select(this)
                .attr("fill", "#0099ff")
                .attr("stroke", "#000000");
            
            // Tooltip sederhana
            svg.append("text")
                .attr("class", "tooltip")
                .attr("x", x(d.category) + x.bandwidth()/2)
                .attr("y", y(d.count) - 10)
                .attr("text-anchor", "middle")
                .style("font-size", "12px")
                .style("font-weight", "bold")
                .text(d.count);
        })
        .on("mouseout", function(event, d) {
            d3.select(this)
                .attr("fill", "#000000")
                .attr("stroke", "#1a1a1a");
            
            d3.selectAll(".tooltip").remove();
        });

    // Label judul sumbu Y
    svg.append("text")
        .attr("transform", "rotate(-90)")
        .attr("y", 0 - margin.left)
        .attr("x", 0 - (chartHeight / 2))
        .attr("dy", "1em")
        .style("text-anchor", "middle")
        .style("font-size", "12px")
        .style("font-weight", "bold")
        .text("NUMBER OF POSTS");
};

window.renderTimeSeriesChart = function(timeData) {
    // Implementasi untuk time series chart (line chart)
    d3.select("#timeSeriesChart").html("");
    
    if (!timeData || timeData.length === 0) {
        d3.select("#timeSeriesChart")
            .append("text")
            .attr("x", "50%")
            .attr("y", "50%")
            .attr("text-anchor", "middle")
            .style("font-size", "14px")
            .text("NO TIME SERIES DATA");
        return;
    }

    const container = d3.select("#timeSeriesChart");
    const width = container.node().clientWidth;
    const height = container.node().clientHeight;
    const margin = { top: 20, right: 30, bottom: 40, left: 40 };
    const chartWidth = width - margin.left - margin.right;
    const chartHeight = height - margin.top - margin.bottom;

    const svg = container.append("svg")
        .attr("width", width)
        .attr("height", height)
        .append("g")
        .attr("transform", `translate(${margin.left},${margin.top})`);

    // Parse date (asumsi format ISO dari .NET)
    const parseTime = d3.timeParse("%Y-%m-%dT%H:%M:%S.%fZ");
    
    const data = timeData.map(d => ({
        time: new Date(d.time),
        count: d.count,
        positive: d.positive,
        negative: d.negative
    })).sort((a, b) => a.time - b.time);

    // Scales
    const x = d3.scaleTime()
        .range([0, chartWidth])
        .domain(d3.extent(data, d => d.time));

    const y = d3.scaleLinear()
        .range([chartHeight, 0])
        .domain([0, d3.max(data, d => Math.max(d.count, d.positive, d.negative)) * 1.2]);

    // Grid lines (Brutalist style)
    svg.append("g")
        .attr("class", "grid")
        .attr("transform", `translate(0,${chartHeight})`)
        .call(d3.axisBottom(x).ticks(5).tickSize(-chartHeight).tickFormat(""))
        .style("stroke-dasharray", "3,3")
        .style("opacity", 0.3);

    svg.append("g")
        .attr("class", "grid")
        .call(d3.axisLeft(y).ticks(5).tickSize(-chartWidth).tickFormat(""))
        .style("stroke-dasharray", "3,3")
        .style("opacity", 0.3);

    // Axes
    svg.append("g")
        .attr("transform", `translate(0,${chartHeight})`)
        .call(d3.axisBottom(x).ticks(5).tickFormat(d3.timeFormat("%H:%M")))
        .selectAll("text")
        .style("font-family", "Arial")
        .style("font-size", "11px");

    svg.append("g")
        .call(d3.axisLeft(y).ticks(5))
        .style("font-family", "Arial")
        .style("font-size", "11px");

    // Line generator
    const line = d3.line()
        .x(d => x(d.time))
        .y(d => y(d.count));

    // Draw main line (Total posts)
    svg.append("path")
        .datum(data)
        .attr("fill", "none")
        .attr("stroke", "#000000")
        .attr("stroke-width", 3)
        .attr("d", line);

    // Draw dots
    svg.selectAll("dot")
        .data(data)
        .enter()
        .append("circle")
        .attr("r", 5)
        .attr("cx", d => x(d.time))
        .attr("cy", d => y(d.count))
        .attr("fill", "#000000")
        .attr("stroke", "#ffffff")
        .attr("stroke-width", 2);

    // Legend sederhana
    svg.append("text")
        .attr("x", 10)
        .attr("y", 20)
        .style("font-size", "12px")
        .style("font-weight", "bold")
        .text("● TOTAL POSTS TREND");
};