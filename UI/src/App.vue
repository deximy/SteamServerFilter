<script setup lang="ts">
declare global
{
    interface Window
    {
        chrome: {
            webview: {
                postMessage: (message: string) => void;
            };
        };
    }
}

</script>

<template>

</template>

<style scoped>

</style>
