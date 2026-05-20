module.exports = {
    apps: [{
        name: 'wms-api',
        script: 'dist/server.js',
        instances: 'max',                // cluster mode: dùng tất cả CPU cores
        exec_mode: 'cluster',
        env_production: {
            NODE_ENV: 'production',
            PORT: 3000,
        },
        // Auto-restart
        max_memory_restart: '450M',
        exp_backoff_restart_delay: 100,

        // Logs
        error_file: './logs/error.log',
        out_file: './logs/out.log',
        merge_logs: true,
        log_date_format: 'YYYY-MM-DD HH:mm:ss Z',

        // Graceful shutdown
        kill_timeout: 5000,
        listen_timeout: 10000,
        shutdown_with_message: true,
    }],
};