import * as esbuild from 'esbuild'
import pkg from './package.json' assert { type: 'json' }
import { copy } from 'esbuild-plugin-copy';

const isProduction = process.env.NODE_ENV === 'production';

await esbuild.build({
    entryPoints: [ pkg.source ],
    bundle: true,
    minify: isProduction,
    outfile: pkg.main,
    sourcemap: !isProduction,
    target: 'es2022',
    format: 'esm',
    define: {
        'process.env.NODE_ENV': `"${process.env.NODE_ENV}"`,
    },
    loader: {
        '.png': 'file'
    },
    plugins: [
        copy({
            assets: [
                {
                    from: ['./node_modules/leaflet/dist/images/*'],
                    to: ['./'],
                }
            ]
        })
    ],
    assetNames: '[name]'
}).catch(() => process.exit(1));
