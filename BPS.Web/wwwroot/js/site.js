(() => {
	const dashboard = document.getElementById("queue-dashboard");
	if (!dashboard) {
		return;
	}

	const submitForm = document.getElementById("submit-form");
	const configForm = document.getElementById("config-form");
	const jobsTableBody = document.querySelector("#jobs-table tbody");
	const priorityFilter = document.getElementById("priority-filter");
	const refreshButton = document.getElementById("refresh-jobs");
	const refreshBadge = document.getElementById("last-refresh");
	const toastHolder = document.getElementById("toast-holder");

	const escapeHtml = (value) => {
    try {
        return (value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "'");
    } catch (error) {
        console.error("HTML escaping failed",value);
        return "";
    }
};

	const showToast = (message, success) => {
		const toast = document.createElement("div");
		toast.className = `toast align-items-center text-white border-0 ${success ? "bg-success" : "bg-danger"}`;
		toast.role = "alert";
		toast.innerHTML = `
			<div class="d-flex">
				<div class="toast-body">${escapeHtml(message)}</div>
				<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
			</div>`;
		toastHolder.appendChild(toast);
		const instance = new bootstrap.Toast(toast, { delay: 2500 });
		instance.show();
		toast.addEventListener("hidden.bs.toast", () => toast.remove());
	};

	const formatDate = (dateValue) => {
		if (!dateValue) {
			return "-";
		}

		const date = new Date(dateValue);
		return date.toLocaleString();
	};

	const statusBadgeClass = (status) => {
		switch (status) {
			case "Queued":
				return "text-bg-secondary";
			case "Running":
				return "text-bg-primary";
			case "Completed":
				return "text-bg-success";
			case "Failed":
				return "text-bg-danger";
			case "Stopped":
				return "text-bg-warning";
			default:
				return "text-bg-light";
		}
	};

	const renderRows = (jobs) => {
		if (!jobs.length) {
			jobsTableBody.innerHTML = "<tr><td colspan='6' class='text-center text-muted'>No jobs found.</td></tr>";
			return;
		}

		jobsTableBody.innerHTML = jobs.map((job) => {
			const errorLine = job.lastError
				? `<div class='small text-danger mt-1'>${escapeHtml(job.lastError)}</div>`
				: "";
			return `
				<tr>
					<td class='small'>${escapeHtml(job.jobId)}</td>
					<td>${escapeHtml(job.jobType)}</td>
					<td>${escapeHtml(job.priority)}</td>
					<td>
						<span class='badge ${statusBadgeClass(job.status)}'>${escapeHtml(job.status)}</span>
						${errorLine}
					</td>
					<td class='small'>${formatDate(job.createdAtUtc)}</td>
					<td>
						<div class='d-flex flex-wrap gap-1'>
							<button class='btn btn-outline-success btn-sm' data-action='start' data-jobid='${escapeHtml(job.jobId)}'>Start</button>
							<button class='btn btn-outline-warning btn-sm' data-action='stop' data-jobid='${escapeHtml(job.jobId)}'>Stop</button>
							<button class='btn btn-outline-info btn-sm' data-action='resume' data-jobid='${escapeHtml(job.jobId)}'>Resume</button>
						</div>
					</td>
				</tr>`;
		}).join("");
	};

	const refreshJobs = async () => {
		const filter = priorityFilter.value;
		const endpoint = filter === "all" ? "/api/queue/list" : `/api/queue/list/${encodeURIComponent(filter)}`;
		const response = await fetch(endpoint);
		if (!response.ok) {
			throw new Error("Could not load jobs.");
		}

		const data = await response.json();
		renderRows(data);
		refreshBadge.textContent = new Date().toLocaleTimeString();
	};

	const loadConfiguration = async () => {
		const response = await fetch("/api/queue/configuration");
		if (!response.ok) {
			throw new Error("Could not load configuration.");
		}

		const config = await response.json();
		document.getElementById("maxConcurrentJobs").value = config.maxConcurrentJobs;
		document.getElementById("maxJobDurationSeconds").value = config.maxJobDurationSeconds;
	};

	submitForm.addEventListener("submit", async (event) => {
		event.preventDefault();
		const payload = {
			jobType: document.getElementById("jobType").value,
			priority: document.getElementById("priority").value,
			payload: document.getElementById("payload").value
		};

		const response = await fetch("/api/queue/submit", {
			method: "POST",
			headers: { "Content-Type": "application/json" },
			body: JSON.stringify(payload)
		});

		if (!response.ok) {
			showToast("Job submission failed.", false);
			return;
		}

		showToast("Job submitted.", true);
		await refreshJobs();
	});

	configForm.addEventListener("submit", async (event) => {
		event.preventDefault();
		const payload = {
			maxConcurrentJobs: Number(document.getElementById("maxConcurrentJobs").value),
			maxJobDurationSeconds: Number(document.getElementById("maxJobDurationSeconds").value)
		};

		const response = await fetch("/api/queue/configuration", {
			method: "POST",
			headers: { "Content-Type": "application/json" },
			body: JSON.stringify(payload)
		});

		if (!response.ok) {
			showToast("Configuration update failed.", false);
			return;
		}

		showToast("Configuration saved.", true);
		await loadConfiguration();
	});

	jobsTableBody.addEventListener("click", async (event) => {
		const target = event.target;
		if (!(target instanceof HTMLElement) || target.tagName !== "BUTTON") {
			return;
		}

		const action = target.dataset.action;
		const jobId = target.dataset.jobid;
		if (!action || !jobId) {
			return;
		}

		const response = await fetch(`/api/queue/status/${encodeURIComponent(jobId)}/${encodeURIComponent(action)}`, {
			method: "POST"
		});

		if (!response.ok) {
			showToast(`Action ${action} failed for ${jobId}.`, false);
			return;
		}

		showToast(`Action ${action} executed for ${jobId}.`, true);
		await refreshJobs();
	});

	priorityFilter.addEventListener("change", async () => {
		await refreshJobs();
	});

	refreshButton.addEventListener("click", async () => {
		await refreshJobs();
	});

	const initialize = async () => {
		try {
			await Promise.all([loadConfiguration(), refreshJobs()]);
			setInterval(() => {
				refreshJobs().catch(() => {
					showToast("Auto-refresh failed.", false);
				});
			}, 3000);
		} catch {
			showToast("Dashboard initialization failed.", false);
		}
	};

	initialize();
})();
